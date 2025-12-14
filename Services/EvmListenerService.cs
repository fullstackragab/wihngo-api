using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using System.Numerics;
using System;

namespace Wihngo.Services;

public interface IEvmListenerService
{
    Task StartListeningAsync(CancellationToken cancellationToken);
}

public class EvmListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BaseConfiguration _config;
    private readonly ILogger<EvmListenerService> _logger;
    private readonly Web3 _web3;

    // ERC-20 Transfer event signature: Transfer(address indexed from, address indexed to, uint256 value)
    private const string TransferEventSignature = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

    public EvmListenerService(
        IServiceProvider serviceProvider,
        IOptions<BaseConfiguration> config,
        ILogger<EvmListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _web3 = new Web3(_config.RpcUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EVM (Base) Listener Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenForTransactionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EVM listener loop");
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

        // Get last processed block
        var cursor = await context.BlockchainCursors
            .FirstOrDefaultAsync(bc => bc.Chain == "base" && bc.CursorType == "block", cancellationToken);

        if (cursor == null)
        {
            cursor = new BlockchainCursor
            {
                Chain = "base",
                CursorType = "block",
                LastProcessedValue = 0
            };
            context.BlockchainCursors.Add(cursor);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Get current block number
        var currentBlockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        var currentBlock = (long)currentBlockNumber.Value;
        var lastBlock = cursor.LastProcessedValue;

        if (currentBlock <= lastBlock)
        {
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        // Only process blocks that have enough confirmations
        var safeBlock = currentBlock - _config.ConfirmationBlocks;
        if (safeBlock <= lastBlock)
        {
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        _logger.LogInformation("Processing Base blocks {LastBlock} to {SafeBlock}", lastBlock, safeBlock);

        // Get supported tokens
        var supportedTokens = await context.SupportedTokens
            .Where(st => st.Chain == "base" && st.IsActive)
            .ToListAsync(cancellationToken);

        if (!supportedTokens.Any())
        {
            _logger.LogWarning("No active Base tokens configured");
            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
            return;
        }

        // Process blocks
        for (var blockNum = lastBlock + 1; blockNum <= safeBlock; blockNum++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ProcessBlockAsync(blockNum, supportedTokens, context, invoiceService, auditService, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Base block {BlockNumber}", blockNum);
            }

            // Update cursor after each block
            cursor.LastProcessedValue = blockNum;
            cursor.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessBlockAsync(
        long blockNumber,
        List<SupportedToken> supportedTokens,
        AppDbContext context,
        IInvoiceService invoiceService,
        IPaymentAuditService auditService,
        CancellationToken cancellationToken)
    {
        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber
            .SendRequestAsync(new BlockParameter((ulong)blockNumber));

        if (block == null)
        {
            _logger.LogWarning("Block {BlockNumber} not found", blockNumber);
            return;
        }

        foreach (var token in supportedTokens)
        {
            try
            {
                await ProcessTokenTransfersInBlockAsync(
                    block,
                    token,
                    context,
                    invoiceService,
                    auditService,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token {Token} in block {BlockNumber}", 
                    token.TokenSymbol, blockNumber);
            }
        }
    }

    private async Task ProcessTokenTransfersInBlockAsync(
        BlockWithTransactions block,
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

        // Get all transactions in the block
        foreach (var transaction in block.Transactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if transaction is to the token contract
                if (!string.Equals(transaction.To, token.MintAddress, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if already processed
                var existingPayment = await context.Payments
                    .FirstOrDefaultAsync(p => p.TxHash == transaction.TransactionHash, cancellationToken);

                if (existingPayment != null)
                    continue;

                // Get transaction receipt to read logs
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt
                    .SendRequestAsync(transaction.TransactionHash);

                if (receipt == null || receipt.Status.Value == 0)
                    continue; // Failed transaction

                // Parse Transfer events
                var transfer = ParseErc20Transfer(receipt, merchantAddress, token);
                if (transfer == null)
                    continue;

                // Find matching invoice
                var invoice = await FindMatchingInvoiceAsync(
                    context,
                    transfer.Amount,
                    token,
                    cancellationToken);

                if (invoice == null)
                {
                    _logger.LogWarning("No matching invoice found for Base transaction {TxHash}", 
                        transaction.TransactionHash);
                    continue;
                }

                // Check tolerance
                var expectedAmount = invoice.AmountFiat; // Should be converted to token amount
                var tolerance = expectedAmount * (token.TolerancePercent / 100m);
                
                if (transfer.Amount < (expectedAmount - tolerance))
                {
                    _logger.LogWarning("Payment amount {Amount} below expected {Expected} for invoice {InvoiceId}",
                        transfer.Amount, expectedAmount, invoice.Id);
                    continue;
                }

                // Record payment
                var payment = new Payment
                {
                    InvoiceId = invoice.Id,
                    PaymentMethod = "base",
                    PayerIdentifier = transfer.From,
                    TxHash = transaction.TransactionHash,
                    Token = token.TokenSymbol,
                    Chain = "base",
                    AmountCrypto = transfer.Amount,
                    BlockSlot = (long)block.Number.Value,
                    Confirmations = _config.ConfirmationBlocks,
                    ConfirmedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Update invoice and record payment
                await invoiceService.RecordPaymentConfirmedAsync(invoice.Id, payment, transfer.Amount);

                _logger.LogInformation("Processed Base payment {TxHash} for invoice {InvoiceId}",
                    transaction.TransactionHash, invoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Base transaction {TxHash}", transaction.TransactionHash);
            }
        }
    }

    private Erc20Transfer? ParseErc20Transfer(TransactionReceipt receipt, string merchantAddress, SupportedToken token)
    {
        // Find Transfer event logs
        var transferLogs = receipt.Logs
            .Where(log => 
                log.Topics != null && 
                log.Topics.Length >= 3 &&
                string.Equals(log.Topics[0].ToString(), TransferEventSignature, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var log in transferLogs)
        {
            try
            {
                // Topics[0] = event signature
                // Topics[1] = from address (indexed)
                // Topics[2] = to address (indexed)
                // Data = amount (not indexed)

                var toAddress = "0x" + log.Topics[2].ToString().Substring(26); // Remove padding

                if (!string.Equals(toAddress, merchantAddress, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fromAddress = "0x" + log.Topics[1].ToString().Substring(26);
                
                // Parse amount from data
                var amountHex = log.Data;
                var amountWei = new HexBigInteger(amountHex).Value;
                var amount = (decimal)amountWei / (decimal)Math.Pow(10, token.Decimals);

                return new Erc20Transfer
                {
                    From = fromAddress,
                    To = toAddress,
                    Amount = amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ERC-20 transfer log");
            }
        }

        return null;
    }

    private async Task<Invoice?> FindMatchingInvoiceAsync(
        AppDbContext context,
        decimal amount,
        SupportedToken token,
        CancellationToken cancellationToken)
    {
        // Find invoices awaiting payment that match the amount
        var invoices = await context.Invoices
            .Where(i => 
                i.State == nameof(InvoicePaymentState.AWAITING_PAYMENT) &&
                i.AmountFiat == amount) // This should be converted properly
            .ToListAsync(cancellationToken);

        return invoices.FirstOrDefault();
    }

    private class Erc20Transfer
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
