using Hangfire;
using Microsoft.EntityFrameworkCore;
using Wihngo.Data;
using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs;

public class PaymentMonitorJob
{
    private readonly AppDbContext _context;
    private readonly IBlockchainService _blockchainService;
    private readonly ICryptoPaymentService _paymentService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentMonitorJob> _logger;

    public PaymentMonitorJob(
        AppDbContext context,
        IBlockchainService blockchainService,
        ICryptoPaymentService paymentService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaymentMonitorJob> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _paymentService = paymentService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task MonitorPendingPaymentsAsync()
    {
        Console.WriteLine("");
        Console.WriteLine("========================================");
        Console.WriteLine("?? PAYMENT MONITOR JOB STARTED");
        Console.WriteLine($"   Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine("========================================");

        try
        {
            // First, check ALL payments to see what we have
            var allPayments = await _context.CryptoPaymentRequests
                .Where(p => p.ExpiresAt > DateTime.UtcNow && p.Status != "completed" && p.Status != "expired" && p.Status != "cancelled")
                .ToListAsync();

            Console.WriteLine($"? Total non-completed payments found: {allPayments.Count}");
            
            foreach (var p in allPayments)
            {
                Console.WriteLine($"   - Payment {p.Id}: Status={p.Status}, TxHash={p.TransactionHash ?? "NULL"}, " +
                    $"Currency={p.Currency}, Network={p.Network}, Amount={p.AmountCrypto}, " +
                    $"WalletAddress={p.WalletAddress}");
            }

            // Monitor payments that already have a transaction hash
            var paymentsWithHash = allPayments.Where(p => p.TransactionHash != null).ToList();

            Console.WriteLine("");
            Console.WriteLine($"?? Payments with transaction hash: {paymentsWithHash.Count}");

            if (paymentsWithHash.Count == 0)
            {
                Console.WriteLine("?? No payments with transaction hash to monitor");
                Console.WriteLine("?? This means either:");
                Console.WriteLine("   1. User hasn't submitted transaction hash yet via verify endpoint");
                Console.WriteLine("   2. Payment was completed or expired");
                Console.WriteLine("   3. No payments exist");
            }

            foreach (var payment in paymentsWithHash)
            {
                try
                {
                    var previousStatus = payment.Status;
                    
                    Console.WriteLine("");
                    Console.WriteLine($"--- Monitoring Payment {payment.Id} ---");
                    Console.WriteLine($"   Status: {previousStatus}");
                    Console.WriteLine($"   Transaction Hash: {payment.TransactionHash}");
                    Console.WriteLine($"   Currency: {payment.Currency}");
                    Console.WriteLine($"   Network: {payment.Network}");
                    Console.WriteLine($"   Confirmations: {payment.Confirmations}/{payment.RequiredConfirmations}");
                    Console.WriteLine($"   Verifying on blockchain...");

                    var txInfo = await _blockchainService.VerifyTransactionAsync(
                        payment.TransactionHash!,
                        payment.Currency,
                        payment.Network
                    );

                    if (txInfo != null)
                    {
                        Console.WriteLine($"   ? Blockchain verification SUCCESS");
                        Console.WriteLine($"   - Confirmations: {txInfo.Confirmations}");
                        Console.WriteLine($"   - Amount: {txInfo.Amount} {payment.Currency}");
                        Console.WriteLine($"   - From: {txInfo.FromAddress}");
                        Console.WriteLine($"   - To: {txInfo.ToAddress}");
                        
                        payment.Confirmations = txInfo.Confirmations;

                        if (txInfo.Confirmations >= payment.RequiredConfirmations)
                        {
                            Console.WriteLine($"   ?? Required confirmations reached!");
                            
                            if (payment.Status == "confirmed")
                            {
                                Console.WriteLine($"   ?? Status is 'confirmed', transitioning to 'completed'...");
                                
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"   ?? PAYMENT {payment.Id} COMPLETED!");
                            }
                            else if (payment.Status != "completed")
                            {
                                Console.WriteLine($"   ?? First time reaching confirmations, setting to 'confirmed' then completing...");
                                
                                payment.Status = "confirmed";
                                payment.ConfirmedAt = DateTime.UtcNow;
                                payment.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                                
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"   ?? PAYMENT {payment.Id} COMPLETED!");
                            }
                            else
                            {
                                Console.WriteLine($"   ?? Already completed, skipping");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"   ? Waiting for more confirmations ({txInfo.Confirmations}/{payment.RequiredConfirmations})");
                            
                            if (payment.Status != "confirming")
                            {
                                payment.Status = "confirming";
                                payment.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"   ?? Status changed to 'confirming'");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ? Transaction NOT FOUND on blockchain");
                        Console.WriteLine($"   ?? This could mean:");
                        Console.WriteLine($"      - Transaction hasn't been mined yet");
                        Console.WriteLine($"      - Wrong network (expected: {payment.Network})");
                        Console.WriteLine($"      - Invalid transaction hash");
                        Console.WriteLine($"      - Blockchain API error");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ? ERROR monitoring payment {payment.Id}:");
                    Console.WriteLine($"   {ex.Message}");
                    _logger.LogError(ex, $"Error monitoring payment {payment.Id}");
                }
            }

            // ============================================
            // ?? PROACTIVE WALLET SCANNING
            // ============================================
            // Check for pending payments without hash - scan their wallets for incoming transactions
            var paymentsWithoutHash = allPayments
                .Where(p => p.TransactionHash == null && p.Status == "pending")
                .Take(20) // Limit to avoid API rate limits
                .ToList();

            if (paymentsWithoutHash.Any())
            {
                Console.WriteLine("");
                Console.WriteLine($"?? {paymentsWithoutHash.Count} pending payments waiting for transaction hash");
                Console.WriteLine($"?? SCANNING WALLETS FOR INCOMING TRANSACTIONS...");
                
                foreach (var payment in paymentsWithoutHash)
                {
                    var timeLeft = payment.ExpiresAt - DateTime.UtcNow;
                    Console.WriteLine($"");
                    Console.WriteLine($"--- Scanning Payment {payment.Id} ---");
                    Console.WriteLine($"   Amount: {payment.AmountCrypto} {payment.Currency}");
                    Console.WriteLine($"   Network: {payment.Network}");
                    Console.WriteLine($"   Wallet: {payment.WalletAddress}");
                    Console.WriteLine($"   HD Index: {payment.AddressIndex?.ToString() ?? "N/A (static wallet)"}");
                    Console.WriteLine($"   Expires in: {timeLeft.TotalMinutes:F1} minutes");

                    try
                    {
                        // Scan wallet for incoming transactions
                        var incomingTx = await ScanWalletForIncomingTransactionAsync(
                            payment.WalletAddress,
                            payment.Currency,
                            payment.Network,
                            payment.AmountCrypto,
                            payment.CreatedAt
                        );

                        if (incomingTx != null)
                        {
                            Console.WriteLine($"   ? INCOMING TRANSACTION DETECTED!");
                            Console.WriteLine($"   - Transaction Hash: {incomingTx.TxHash}");
                            Console.WriteLine($"   - Amount: {incomingTx.Amount} {payment.Currency}");
                            Console.WriteLine($"   - From: {incomingTx.FromAddress}");
                            Console.WriteLine($"   - Confirmations: {incomingTx.Confirmations}");
                            Console.WriteLine($"   - HD Derivation: Index {payment.AddressIndex?.ToString() ?? "N/A"}");

                            // Update payment with discovered transaction
                            payment.TransactionHash = incomingTx.TxHash;
                            payment.UserWalletAddress = incomingTx.FromAddress;
                            payment.Confirmations = incomingTx.Confirmations;
                            payment.Status = incomingTx.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "confirming";
                            payment.UpdatedAt = DateTime.UtcNow;

                            if (payment.Status == "confirmed")
                            {
                                payment.ConfirmedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();

                                // Complete payment immediately
                                Console.WriteLine($"   ? Payment has sufficient confirmations, completing now...");
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();
                                Console.WriteLine($"   ? PAYMENT {payment.Id} AUTO-COMPLETED!");
                            }
                            else
                            {
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"   ? Transaction found but waiting for confirmations ({incomingTx.Confirmations}/{payment.RequiredConfirmations})");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"   ? No matching transaction found yet");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ? ERROR scanning wallet: {ex.Message}");
                        _logger.LogError(ex, $"Error scanning wallet for payment {payment.Id}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? MONITOR JOB ERROR: {ex.Message}");
            _logger.LogError(ex, "Error in payment monitoring job");
            throw;
        }
        
        Console.WriteLine("");
        Console.WriteLine("========================================");
        Console.WriteLine("?? PAYMENT MONITOR JOB COMPLETED");
        Console.WriteLine($"   Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine("========================================");
        Console.WriteLine("");
    }

    /// <summary>
    /// Scan a wallet address for incoming transactions matching the expected amount
    /// </summary>
    private async Task<IncomingTransaction?> ScanWalletForIncomingTransactionAsync(
        string walletAddress,
        string currency,
        string network,
        decimal expectedAmount,
        DateTime since)
    {
        try
        {
            if (network.ToLower() == "tron")
            {
                return await ScanTronWalletAsync(walletAddress, currency, expectedAmount, since);
            }
            // Add support for other networks as needed
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning wallet {walletAddress} on {network}");
            return null;
        }
    }

    /// <summary>
    /// Scan Tron wallet for incoming USDT/TRX transactions
    /// </summary>
    private async Task<IncomingTransaction?> ScanTronWalletAsync(
        string walletAddress,
        string currency,
        decimal expectedAmount,
        DateTime since)
    {
        try
        {
            var apiUrl = _configuration["BlockchainSettings:TronGrid:ApiUrl"] ?? "https://api.trongrid.io";
            var apiKey = _configuration["BlockchainSettings:TronGrid:ApiKey"];
            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", apiKey);
            }

            if (currency.ToUpper() == "USDT")
            {
                // TRC-20 USDT - use TronGrid TRC-20 transaction API
                // USDT contract: TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t
                var usdtContract = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
                
                // Get TRC-20 transactions for this address
                var url = $"{apiUrl}/v1/accounts/{walletAddress}/transactions/trc20?only_to=true&limit=20&contract_address={usdtContract}";
                
                // Mask API key in logs if present
                var logUrl = !string.IsNullOrEmpty(apiKey) ? url.Replace(apiKey, "***") : url;
                Console.WriteLine($"   ?? Scanning Tron wallet via: {logUrl}");
                
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   ?? TronGrid API returned {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonDocument.Parse(json);

                if (!data.RootElement.TryGetProperty("data", out var transactions))
                {
                    Console.WriteLine($"   ?? No 'data' property in response");
                    return null;
                }

                Console.WriteLine($"   ?? Found {transactions.GetArrayLength()} recent TRC-20 transactions");

                // Look for matching transaction
                var sinceTimestamp = new DateTimeOffset(since).ToUnixTimeMilliseconds();
                var tolerance = expectedAmount * 0.01m; // 1% tolerance

                foreach (var tx in transactions.EnumerateArray())
                {
                    var timestamp = tx.GetProperty("block_timestamp").GetInt64();
                    
                    // Skip transactions before payment was created
                    if (timestamp < sinceTimestamp)
                    {
                        continue;
                    }

                    var to = tx.GetProperty("to").GetString();
                    var value = tx.GetProperty("value").GetString();
                    var txId = tx.GetProperty("transaction_id").GetString();

                    if (to?.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) == true && 
                        !string.IsNullOrEmpty(value) && 
                        !string.IsNullOrEmpty(txId))
                    {
                        // TRC-20 USDT has 6 decimals
                        var amount = decimal.Parse(value) / 1_000_000m;
                        
                        Console.WriteLine($"   ?? Checking tx {txId}: {amount} USDT");

                        // Check if amount matches (within tolerance)
                        if (Math.Abs(amount - expectedAmount) <= tolerance)
                        {
                            Console.WriteLine($"   ? AMOUNT MATCHES! ({amount} ? {expectedAmount})");

                            // Verify transaction to get confirmations
                            var txInfo = await _blockchainService.VerifyTransactionAsync(txId, currency, "tron");
                            
                            if (txInfo != null)
                            {
                                return new IncomingTransaction
                                {
                                    TxHash = txId,
                                    Amount = amount,
                                    FromAddress = txInfo.FromAddress,
                                    ToAddress = to,
                                    Confirmations = txInfo.Confirmations,
                                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
                                };
                            }
                        }
                    }
                }

                Console.WriteLine($"   ? No matching transaction found for {expectedAmount} USDT");
            }
            else if (currency.ToUpper() == "TRX")
            {
                // Native TRX transfers
                var url = $"{apiUrl}/v1/accounts/{walletAddress}/transactions?only_to=true&limit=20";
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonDocument.Parse(json);

                if (!data.RootElement.TryGetProperty("data", out var transactions))
                {
                    return null;
                }

                var sinceTimestamp = new DateTimeOffset(since).ToUnixTimeMilliseconds();
                var tolerance = expectedAmount * 0.01m;

                foreach (var tx in transactions.EnumerateArray())
                {
                    var timestamp = tx.GetProperty("block_timestamp").GetInt64();
                    if (timestamp < sinceTimestamp) continue;

                    var txId = tx.GetProperty("txID").GetString();
                    if (string.IsNullOrEmpty(txId)) continue;

                    var txInfo = await _blockchainService.VerifyTransactionAsync(txId, currency, "tron");
                    
                    if (txInfo != null && 
                        txInfo.ToAddress.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(txInfo.Amount - expectedAmount) <= tolerance)
                    {
                        return new IncomingTransaction
                        {
                            TxHash = txId,
                            Amount = txInfo.Amount,
                            FromAddress = txInfo.FromAddress,
                            ToAddress = txInfo.ToAddress,
                            Confirmations = txInfo.Confirmations,
                            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
                        };
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning Tron wallet {walletAddress}");
            return null;
        }
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExpireOldPaymentsAsync()
    {
        _logger.LogInformation("Expiring old payments...");

        try
        {
            var expiredCount = await _context.CryptoPaymentRequests
                .Where(p => p.Status == "pending" && p.ExpiresAt < DateTime.UtcNow)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.Status, "expired")
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            _logger.LogInformation($"Expired {expiredCount} payments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring payments");
            throw;
        }
    }
}

/// <summary>
/// Represents an incoming transaction detected by wallet scanning
/// </summary>
public class IncomingTransaction
{
    public required string TxHash { get; set; }
    public decimal Amount { get; set; }
    public required string FromAddress { get; set; }
    public required string ToAddress { get; set; }
    public int Confirmations { get; set; }
    public DateTime Timestamp { get; set; }
}
