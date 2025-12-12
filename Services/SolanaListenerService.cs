using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Solnet.Rpc;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;

namespace Wihngo.Services;

public interface ISolanaListenerService
{
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task ProcessTransactionAsync(string signature);
}

public class SolanaListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SolanaConfiguration _config;
    private readonly ILogger<SolanaListenerService> _logger;
    private readonly IRpcClient _rpcClient;

    public SolanaListenerService(
        IServiceProvider serviceProvider,
        IOptions<SolanaConfiguration> config,
        ILogger<SolanaListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _rpcClient = ClientFactory.GetClient(_config.RpcUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Solana Listener Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenForTransactionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Solana listener loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ListenForTransactionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IPaymentAuditService>();

        // Get last processed slot
        var cursor = await context.BlockchainCursors
            .FirstOrDefaultAsync(bc => bc.Chain == "solana" && bc.CursorType == "slot", cancellationToken);

        if (cursor == null)
        {
            cursor = new BlockchainCursor
            {
                Chain = "solana",
                CursorType = "slot",
                LastProcessedValue = 0
            };
            context.BlockchainCursors.Add(cursor);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Get current slot
        var slotResponse = await _rpcClient.GetSlotAsync(Commitment.Finalized);
        if (!slotResponse.WasSuccessful)
        {
            _logger.LogWarning("Failed to get current Solana slot");
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        var currentSlot = slotResponse.Result;
        var lastSlot = cursor.LastProcessedValue;

        if (currentSlot <= (ulong)lastSlot)
        {
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        _logger.LogInformation("Processing Solana slots {LastSlot} to {CurrentSlot}", lastSlot, currentSlot);

        // Get supported tokens
        var supportedTokens = await context.SupportedTokens
            .Where(st => st.Chain == "solana" && st.IsActive)
            .ToListAsync(cancellationToken);

        if (!supportedTokens.Any())
        {
            _logger.LogWarning("No active Solana tokens configured");
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        // Process transactions for each supported token
        foreach (var token in supportedTokens)
        {
            try
            {
                await ProcessTokenTransactionsAsync(token, context, invoiceService, auditService, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Solana token {Token}", token.TokenSymbol);
            }
        }

        // Update cursor
        cursor.LastProcessedValue = (long)currentSlot;
        cursor.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessTokenTransactionsAsync(
        SupportedToken token,
        AppDbContext context,
        IInvoiceService invoiceService,
        IPaymentAuditService auditService,
        CancellationToken cancellationToken)
    {
        var merchantAddress = token.MerchantReceivingAddress ?? _config.MerchantWalletAddress;
        if (string.IsNullOrEmpty(merchantAddress))
        {
            _logger.LogWarning("No merchant address configured for {Token}", token.TokenSymbol);
            return;
        }

        // Get recent transactions for the merchant address
        var signaturesResponse = await _rpcClient.GetSignaturesForAddressAsync(
            merchantAddress,
            limit: 100,
            commitment: Commitment.Finalized);

        if (!signaturesResponse.WasSuccessful)
        {
            _logger.LogWarning("Failed to get signatures for {Address}", merchantAddress);
            return;
        }

        foreach (var signatureInfo in signaturesResponse.Result)
        {
            try
            {
                // Check if already processed
                var existingPayment = await context.Payments
                    .FirstOrDefaultAsync(p => p.TxHash == signatureInfo.Signature, cancellationToken);

                if (existingPayment != null)
                    continue; // Already processed

                // Get transaction details
                var txResponse = await _rpcClient.GetTransactionAsync(
                    signatureInfo.Signature,
                    Commitment.Finalized);

                if (!txResponse.WasSuccessful || txResponse.Result == null)
                    continue;

                var transaction = txResponse.Result;

                // Parse transaction to find SPL token transfers
                var tokenTransfer = ParseSplTokenTransfer(transaction, merchantAddress, token.MintAddress);
                if (tokenTransfer == null)
                    continue;

                // Find matching invoice by reference or amount
                var invoice = await FindMatchingInvoiceAsync(
                    context,
                    tokenTransfer.Reference,
                    tokenTransfer.Amount,
                    token,
                    cancellationToken);

                if (invoice == null)
                {
                    _logger.LogWarning("No matching invoice found for Solana transaction {Signature}", 
                        signatureInfo.Signature);
                    continue;
                }

                // Check tolerance
                var expectedAmount = invoice.AmountFiat; // This should be converted to token amount
                var tolerance = expectedAmount * (token.TolerancePercent / 100m);
                
                if (tokenTransfer.Amount < (expectedAmount - tolerance))
                {
                    _logger.LogWarning("Payment amount {Amount} below expected {Expected} for invoice {InvoiceId}",
                        tokenTransfer.Amount, expectedAmount, invoice.Id);
                    continue;
                }

                // Record payment
                var payment = new Payment
                {
                    InvoiceId = invoice.Id,
                    PaymentMethod = "solana",
                    PayerIdentifier = tokenTransfer.Sender,
                    TxHash = signatureInfo.Signature,
                    Token = token.TokenSymbol,
                    Chain = "solana",
                    AmountCrypto = tokenTransfer.Amount,
                    BlockSlot = (long)signatureInfo.Slot,
                    Confirmations = _config.ConfirmationBlocks,
                    ConfirmedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Update invoice and record payment
                await invoiceService.RecordPaymentConfirmedAsync(invoice.Id, payment, tokenTransfer.Amount);

                _logger.LogInformation("Processed Solana payment {Signature} for invoice {InvoiceId}",
                    signatureInfo.Signature, invoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Solana signature {Signature}", signatureInfo.Signature);
            }
        }
    }

    private SplTokenTransfer? ParseSplTokenTransfer(TransactionMetaInfo transaction, string merchantAddress, string mintAddress)
    {
        // Simple parsing logic - in production, use proper SPL token parsing
        // This is a placeholder that would need proper implementation with Solnet's token parsing
        
        // For now, return null to indicate parsing is needed
        // Real implementation would parse preTokenBalances and postTokenBalances
        // to determine transfer amounts and accounts
        
        return null;
    }

    private async Task<Invoice?> FindMatchingInvoiceAsync(
        AppDbContext context,
        string? reference,
        decimal amount,
        SupportedToken token,
        CancellationToken cancellationToken)
    {
        // Try to find by Solana reference first
        if (!string.IsNullOrEmpty(reference))
        {
            var invoice = await context.Invoices
                .FirstOrDefaultAsync(i => 
                    i.SolanaReference == reference && 
                    i.State == nameof(InvoicePaymentState.AWAITING_PAYMENT),
                    cancellationToken);

            if (invoice != null)
                return invoice;
        }

        // Try to find by amount (less reliable)
        var invoices = await context.Invoices
            .Where(i => 
                i.State == nameof(InvoicePaymentState.AWAITING_PAYMENT) &&
                i.AmountFiat == amount)
            .ToListAsync(cancellationToken);

        return invoices.FirstOrDefault();
    }

    private class SplTokenTransfer
    {
        public string Sender { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
