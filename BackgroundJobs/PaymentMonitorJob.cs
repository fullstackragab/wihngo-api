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
        _logger.LogInformation("Monitoring pending payments...");

        try
        {
            // Monitor payments that already have a transaction hash
            var paymentsWithHash = await _context.CryptoPaymentRequests
                .Where(p => (p.Status == "pending" || p.Status == "confirming") &&
                           p.ExpiresAt > DateTime.UtcNow &&
                           p.TransactionHash != null)
                .ToListAsync();

            _logger.LogInformation($"Found {paymentsWithHash.Count} payments with transaction hash to monitor");

            foreach (var payment in paymentsWithHash)
            {
                try
                {
                    var txInfo = await _blockchainService.VerifyTransactionAsync(
                        payment.TransactionHash!,
                        payment.Currency,
                        payment.Network
                    );

                    if (txInfo != null)
                    {
                        payment.Confirmations = txInfo.Confirmations;

                        if (txInfo.Confirmations >= payment.RequiredConfirmations)
                        {
                            payment.Status = "confirmed";
                            payment.ConfirmedAt = DateTime.UtcNow;
                            payment.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync();
                            await _paymentService.CompletePaymentAsync(payment);

                            _logger.LogInformation($"Payment {payment.Id} confirmed and completed with {txInfo.Confirmations} confirmations");
                        }
                        else
                        {
                            payment.Status = "confirming";
                            payment.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation($"Payment {payment.Id} has {txInfo.Confirmations}/{payment.RequiredConfirmations} confirmations");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Transaction {payment.TransactionHash} not found on blockchain for payment {payment.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error monitoring payment {payment.Id} with tx {payment.TransactionHash}");
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
