using System;
using Dapper;
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
    private readonly IDbConnectionFactory _dbFactory;

    public SolanaListenerService(
        IServiceProvider serviceProvider,
        IOptions<SolanaConfiguration> config,
        ILogger<SolanaListenerService> logger,
        IDbConnectionFactory dbFactory)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _dbFactory = dbFactory;
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
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IPaymentAuditService>();

        using var connection = await _dbFactory.CreateOpenConnectionAsync();

        // Get last processed slot
        var cursor = await connection.QueryFirstOrDefaultAsync<BlockchainCursor>(
            "SELECT * FROM blockchain_cursors WHERE chain = @Chain AND cursor_type = @CursorType LIMIT 1",
            new { Chain = "solana", CursorType = "slot" });

        if (cursor == null)
        {
            cursor = new BlockchainCursor
            {
                Id = Guid.NewGuid(),
                Chain = "solana",
                CursorType = "slot",
                LastProcessedValue = 0,
                UpdatedAt = DateTime.UtcNow
            };
            await connection.ExecuteAsync(
                @"INSERT INTO blockchain_cursors (id, chain, cursor_type, last_processed_value, updated_at)
                  VALUES (@Id, @Chain, @CursorType, @LastProcessedValue, @UpdatedAt)",
                cursor);
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
        var supportedTokens = (await connection.QueryAsync<SupportedToken>(
            "SELECT * FROM supported_tokens WHERE chain = @Chain AND is_active = @IsActive",
            new { Chain = "solana", IsActive = true })).ToList();

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
                await ProcessTokenTransactionsAsync(token, connection, invoiceService, auditService, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Solana token {Token}", token.TokenSymbol);
            }
        }

        // Update cursor
        await connection.ExecuteAsync(
            "UPDATE blockchain_cursors SET last_processed_value = @LastProcessedValue, updated_at = @UpdatedAt WHERE id = @Id",
            new { LastProcessedValue = (long)currentSlot, UpdatedAt = DateTime.UtcNow, Id = cursor.Id });
    }

    private async Task ProcessTokenTransactionsAsync(
        SupportedToken token,
        System.Data.IDbConnection connection,
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
                var existingPayment = await connection.QueryFirstOrDefaultAsync<Payment>(
                    "SELECT * FROM payments WHERE tx_hash = @TxHash LIMIT 1",
                    new { TxHash = signatureInfo.Signature });

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
                    connection,
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
        System.Data.IDbConnection connection,
        string? reference,
        decimal amount,
        SupportedToken token,
        CancellationToken cancellationToken)
    {
        // Try to find by Solana reference first
        if (!string.IsNullOrEmpty(reference))
        {
            var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(
                "SELECT * FROM invoices WHERE solana_reference = @Reference AND state = @State LIMIT 1",
                new { Reference = reference, State = nameof(InvoicePaymentState.AWAITING_PAYMENT) });

            if (invoice != null)
                return invoice;
        }

        // Try to find by amount (less reliable)
        var invoices = await connection.QueryAsync<Invoice>(
            "SELECT * FROM invoices WHERE state = @State AND amount_fiat = @Amount",
            new { State = nameof(InvoicePaymentState.AWAITING_PAYMENT), Amount = amount });

        return invoices.FirstOrDefault();
    }

    private class SplTokenTransfer
    {
        public string Sender { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
