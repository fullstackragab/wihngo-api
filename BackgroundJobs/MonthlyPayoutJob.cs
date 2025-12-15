using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs
{
    /// <summary>
    /// Background job that processes monthly payouts on the 1st of each month
    /// </summary>
    public class MonthlyPayoutJob
    {
        private readonly IPayoutService _payoutService;
        private readonly ILogger<MonthlyPayoutJob> _logger;

        public MonthlyPayoutJob(
            IPayoutService payoutService,
            ILogger<MonthlyPayoutJob> logger)
        {
            _payoutService = payoutService;
            _logger = logger;
        }

        /// <summary>
        /// Execute monthly payout processing
        /// This should be scheduled to run on the 1st of each month at 00:00 UTC
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting monthly payout job at {Time}", DateTime.UtcNow);

            try
            {
                var result = await _payoutService.ProcessPayoutsAsync();

                _logger.LogInformation(
                    "Monthly payout job completed. Processed: {Processed}, Successful: {Successful}, Failed: {Failed}, Total Amount: €{Amount}",
                    result.Processed,
                    result.Successful,
                    result.Failed,
                    result.TotalAmount);

                // Log details of failed payouts
                if (result.Failed > 0)
                {
                    _logger.LogWarning("Failed payouts:");
                    foreach (var detail in result.Details)
                    {
                        if (detail.Status == "failed")
                        {
                            _logger.LogWarning(
                                "  User {UserId}: {Amount} EUR - {Error}",
                                detail.UserId,
                                detail.Amount,
                                detail.Error);
                        }
                    }
                }

                // TODO: Send email notifications to users about their payouts
                // This can be done by calling an email notification service
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing monthly payout job");
                throw;
            }
        }

        /// <summary>
        /// Test method to manually trigger payout processing (for admin use)
        /// </summary>
        public async Task ExecuteManualAsync(Guid? userId = null)
        {
            var userContext = userId.HasValue ? $" for user {userId}" : " for all eligible users";
            _logger.LogInformation("Starting manual payout job{Context} at {Time}", userContext, DateTime.UtcNow);

            try
            {
                var result = await _payoutService.ProcessPayoutsAsync(userId);

                _logger.LogInformation(
                    "Manual payout job completed. Processed: {Processed}, Successful: {Successful}, Failed: {Failed}",
                    result.Processed,
                    result.Successful,
                    result.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing manual payout job");
                throw;
            }
        }
    }
}
