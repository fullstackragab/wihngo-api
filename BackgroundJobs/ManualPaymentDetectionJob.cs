using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs;

/// <summary>
/// Background job that polls Solana for incoming USDC deposits to manual payment addresses.
/// This job detects when users have sent USDC to their unique HD-derived payment addresses
/// and confirms the payments automatically.
/// </summary>
public class ManualPaymentDetectionJob
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISolanaTransactionService _solanaService;
    private readonly ILogger<ManualPaymentDetectionJob> _logger;

    public ManualPaymentDetectionJob(
        IPaymentRepository paymentRepository,
        ISolanaTransactionService solanaService,
        ILogger<ManualPaymentDetectionJob> logger)
    {
        _paymentRepository = paymentRepository;
        _solanaService = solanaService;
        _logger = logger;
    }

    /// <summary>
    /// Checks all pending manual payments for incoming USDC deposits.
    /// Should be run every 10-15 seconds via Hangfire.
    /// </summary>
    public async Task CheckPendingManualPaymentsAsync()
    {
        try
        {
            var pendingPayments = await _paymentRepository.GetPendingManualPaymentsAsync();

            if (pendingPayments.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Checking {Count} pending manual payments for deposits", pendingPayments.Count);

            foreach (var payment in pendingPayments)
            {
                try
                {
                    // Check if payment has expired
                    if (payment.ExpiresAt.HasValue && DateTime.UtcNow > payment.ExpiresAt.Value)
                    {
                        _logger.LogInformation(
                            "Manual payment {PaymentId} expired at {ExpiresAt}",
                            payment.Id, payment.ExpiresAt);

                        await _paymentRepository.ExpireAsync(payment.Id);
                        continue;
                    }

                    // Check USDC balance at the destination address
                    if (string.IsNullOrEmpty(payment.DestinationAddress))
                    {
                        _logger.LogWarning(
                            "Manual payment {PaymentId} has no destination address",
                            payment.Id);
                        continue;
                    }

                    var balance = await _solanaService.GetUsdcBalanceAsync(payment.DestinationAddress);

                    // Convert expected amount from cents to USDC (100 cents = 1 USDC)
                    var expectedAmountUsdc = payment.AmountCents / 100m;

                    _logger.LogDebug(
                        "Payment {PaymentId}: address={Address}, balance={Balance} USDC, expected={Expected} USDC",
                        payment.Id, payment.DestinationAddress, balance, expectedAmountUsdc);

                    if (balance >= expectedAmountUsdc)
                    {
                        // Payment received! Confirm it.
                        // Use a generated provider reference since we don't have the exact tx hash
                        var providerRef = $"manual-deposit-{payment.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        await _paymentRepository.ConfirmAsync(payment.Id, providerRef, DateTime.UtcNow);

                        _logger.LogInformation(
                            "Manual payment {PaymentId} confirmed! Received {Balance} USDC at {Address}",
                            payment.Id, balance, payment.DestinationAddress);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error checking manual payment {PaymentId} at address {Address}",
                        payment.Id, payment.DestinationAddress);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ManualPaymentDetectionJob");
        }
    }
}
