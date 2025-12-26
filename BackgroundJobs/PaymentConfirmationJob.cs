using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs;

/// <summary>
/// Background job that polls Solana for payment confirmations
/// </summary>
public class PaymentConfirmationJob
{
    private readonly IP2PPaymentService _paymentService;
    private readonly ILogger<PaymentConfirmationJob> _logger;

    public PaymentConfirmationJob(
        IP2PPaymentService paymentService,
        ILogger<PaymentConfirmationJob> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Checks all pending payments for confirmation
    /// Should be run every 5-10 seconds via Hangfire
    /// </summary>
    public async Task CheckPendingPaymentsAsync()
    {
        try
        {
            var pendingPayments = await _paymentService.GetPendingConfirmationsAsync();

            if (pendingPayments.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Checking {Count} pending payments for confirmation", pendingPayments.Count);

            foreach (var payment in pendingPayments)
            {
                try
                {
                    var confirmed = await _paymentService.CheckPaymentConfirmationAsync(payment.Id);

                    if (confirmed)
                    {
                        _logger.LogInformation(
                            "Payment {PaymentId} confirmed and completed: {Amount} USDC",
                            payment.Id, payment.AmountUsdc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking confirmation for payment {PaymentId}", payment.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PaymentConfirmationJob");
        }
    }
}
