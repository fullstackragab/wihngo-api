using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs;

/// <summary>
/// Hangfire background job for processing weekly support reminders.
///
/// Runs two operations:
/// 1. ProcessRemindersAsync - Every 15 minutes, sends due reminders
/// 2. ExpireOldRemindersAsync - Every hour, expires 7-day-old payments
/// </summary>
public class WeeklySupportReminderJob
{
    private readonly IWeeklySupportService _weeklySupportService;
    private readonly ILogger<WeeklySupportReminderJob> _logger;

    public WeeklySupportReminderJob(
        IWeeklySupportService weeklySupportService,
        ILogger<WeeklySupportReminderJob> logger)
    {
        _weeklySupportService = weeklySupportService;
        _logger = logger;
    }

    /// <summary>
    /// Processes due reminders for active subscriptions.
    /// Called every 15 minutes by Hangfire.
    /// </summary>
    public async Task ProcessRemindersAsync()
    {
        _logger.LogDebug("Starting weekly support reminder processing");

        try
        {
            await _weeklySupportService.ProcessWeeklyRemindersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weekly support reminders");
            throw; // Re-throw so Hangfire can retry
        }
    }

    /// <summary>
    /// Expires old payment reminders that weren't approved.
    /// Called every hour by Hangfire.
    /// </summary>
    public async Task ExpireOldRemindersAsync()
    {
        _logger.LogDebug("Starting weekly support expiration check");

        try
        {
            await _weeklySupportService.ExpireOldPaymentRemindersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring weekly support reminders");
            throw; // Re-throw so Hangfire can retry
        }
    }
}
