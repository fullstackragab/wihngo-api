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
    private readonly ILogger<PaymentMonitorJob> _logger;

    public PaymentMonitorJob(
        AppDbContext context,
        IBlockchainService blockchainService,
        ICryptoPaymentService paymentService,
        ILogger<PaymentMonitorJob> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _paymentService = paymentService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task MonitorPendingPaymentsAsync()
    {
        _logger.LogInformation("=== MONITORING PENDING PAYMENTS ===");

        try
        {
            // Monitor payments that already have a transaction hash
            var paymentsWithHash = await _context.CryptoPaymentRequests
                .Where(p => (p.Status == "pending" || p.Status == "confirming" || p.Status == "confirmed") &&
                           p.ExpiresAt > DateTime.UtcNow &&
                           p.TransactionHash != null)
                .ToListAsync();

            _logger.LogInformation($"Found {paymentsWithHash.Count} payments with transaction hash to monitor");

            foreach (var payment in paymentsWithHash)
            {
                try
                {
                    var previousStatus = payment.Status;
                    _logger.LogInformation($"[Payment {payment.Id}] Current status: {previousStatus}, Confirmations: {payment.Confirmations}/{payment.RequiredConfirmations}");

                    var txInfo = await _blockchainService.VerifyTransactionAsync(
                        payment.TransactionHash!,
                        payment.Currency,
                        payment.Network
                    );

                    if (txInfo != null)
                    {
                        payment.Confirmations = txInfo.Confirmations;
                        _logger.LogInformation($"[Payment {payment.Id}] Blockchain verification: {txInfo.Confirmations} confirmations");

                        if (txInfo.Confirmations >= payment.RequiredConfirmations)
                        {
                            if (payment.Status == "confirmed")
                            {
                                // Payment is confirmed but not completed yet - complete it now
                                _logger.LogInformation($"[Payment {payment.Id}] Status is 'confirmed', transitioning to 'completed'");
                                
                                await _paymentService.CompletePaymentAsync(payment);
                                
                                // Reload to get updated status
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"? SUCCESS: Payment {payment.Id} completed successfully!");
                                Console.WriteLine($"   User: {payment.UserId}");
                                Console.WriteLine($"   Amount: {payment.AmountCrypto} {payment.Currency} (${payment.AmountUsd})");
                                Console.WriteLine($"   Transaction: {payment.TransactionHash}");
                                Console.WriteLine($"   Confirmations: {payment.Confirmations}/{payment.RequiredConfirmations}");
                                Console.WriteLine($"   Status: {previousStatus} -> {payment.Status}");
                                Console.WriteLine($"   Completed At: {payment.CompletedAt}");
                                
                                _logger.LogInformation($"[Payment {payment.Id}] ? COMPLETED SUCCESSFULLY - Status changed from '{previousStatus}' to '{payment.Status}'");
                            }
                            else if (payment.Status != "completed")
                            {
                                // First time reaching required confirmations - confirm it
                                payment.Status = "confirmed";
                                payment.ConfirmedAt = DateTime.UtcNow;
                                payment.UpdatedAt = DateTime.UtcNow;

                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"[Payment {payment.Id}] Status changed from '{previousStatus}' to 'confirmed' with {txInfo.Confirmations} confirmations");
                                
                                // Now complete it
                                await _paymentService.CompletePaymentAsync(payment);
                                
                                // Reload to get updated status
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"? SUCCESS: Payment {payment.Id} completed successfully!");
                                Console.WriteLine($"   User: {payment.UserId}");
                                Console.WriteLine($"   Amount: {payment.AmountCrypto} {payment.Currency} (${payment.AmountUsd})");
                                Console.WriteLine($"   Transaction: {payment.TransactionHash}");
                                Console.WriteLine($"   Confirmations: {payment.Confirmations}/{payment.RequiredConfirmations}");
                                Console.WriteLine($"   Status: {previousStatus} -> {payment.Status}");
                                Console.WriteLine($"   Completed At: {payment.CompletedAt}");
                                
                                _logger.LogInformation($"[Payment {payment.Id}] ? COMPLETED SUCCESSFULLY - Status changed from '{previousStatus}' to '{payment.Status}'");
                            }
                            else
                            {
                                _logger.LogInformation($"[Payment {payment.Id}] Already completed");
                            }
                        }
                        else
                        {
                            if (payment.Status != "confirming")
                            {
                                payment.Status = "confirming";
                                payment.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"[Payment {payment.Id}] Status changed from '{previousStatus}' to 'confirming' - waiting for confirmations ({txInfo.Confirmations}/{payment.RequiredConfirmations})");
                            }
                            else
                            {
                                _logger.LogInformation($"[Payment {payment.Id}] Still confirming - {txInfo.Confirmations}/{payment.RequiredConfirmations} confirmations");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[Payment {payment.Id}] Transaction {payment.TransactionHash} not found on blockchain");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Payment {payment.Id}] Error monitoring payment with tx {payment.TransactionHash}");
                }
            }

            // Also check for pending payments without hash that might have received funds
            // This helps catch cases where users sent payment but didn't call verify endpoint
            var paymentsWithoutHash = await _context.CryptoPaymentRequests
                .Where(p => p.Status == "pending" &&
                           p.ExpiresAt > DateTime.UtcNow &&
                           p.TransactionHash == null)
                .Take(50) // Limit to avoid overload
                .ToListAsync();

            if (paymentsWithoutHash.Any())
            {
                _logger.LogInformation($"Checking {paymentsWithoutHash.Count} payments without transaction hash for incoming funds");

                // Note: Proactive blockchain scanning would require chain-specific APIs
                // For now, we log this for visibility. Implementation would need:
                // - Wallet address monitoring via blockchain explorers
                // - Websocket connections to blockchain nodes
                // - Third-party services like Tatum, Moralis, etc.
                
                foreach (var payment in paymentsWithoutHash)
                {
                    _logger.LogDebug($"Payment {payment.Id} waiting for funds to {payment.WalletAddress} ({payment.Currency} on {payment.Network})");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment monitoring job");
            throw;
        }
        
        _logger.LogInformation("=== PAYMENT MONITORING COMPLETED ===");
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
