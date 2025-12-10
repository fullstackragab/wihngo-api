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
            var payments = await _context.CryptoPaymentRequests
                .Where(p => (p.Status == "pending" || p.Status == "confirming") &&
                           p.ExpiresAt > DateTime.UtcNow &&
                           p.TransactionHash != null)
                .ToListAsync();

            _logger.LogInformation($"Found {payments.Count} payments to monitor");

            foreach (var payment in payments)
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

                            _logger.LogInformation($"Payment {payment.Id} confirmed and completed");
                        }
                        else
                        {
                            payment.Status = "confirming";
                            payment.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation($"Payment {payment.Id} has {txInfo.Confirmations}/{payment.RequiredConfirmations} confirmations");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error monitoring payment {payment.Id}");
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
