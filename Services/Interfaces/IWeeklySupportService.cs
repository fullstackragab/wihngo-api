using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing weekly support subscriptions.
///
/// Non-Custodial Recurring Donations:
/// - User subscribes to weekly bird support
/// - Each week: reminder notification sent
/// - User approves with 1-click in Phantom wallet
/// - Bird owner receives 100% of bird amount
///
/// Wihngo never holds or controls user funds.
/// </summary>
public interface IWeeklySupportService
{
    // =============================================
    // SUBSCRIPTION MANAGEMENT
    // =============================================

    /// <summary>
    /// Creates a new weekly support subscription for a bird.
    /// Validates: bird exists, user has wallet, user isn't owner.
    /// </summary>
    Task<(WeeklySupportSubscriptionResponse? Response, ValidationErrorResponse? Error)> CreateSubscriptionAsync(
        Guid userId,
        CreateWeeklySupportRequest request);

    /// <summary>
    /// Gets a subscription by ID (user must be subscriber)
    /// </summary>
    Task<WeeklySupportSubscriptionResponse?> GetSubscriptionAsync(
        Guid subscriptionId,
        Guid userId);

    /// <summary>
    /// Gets all subscriptions for a user
    /// </summary>
    Task<List<WeeklySupportSummaryDto>> GetUserSubscriptionsAsync(Guid userId);

    /// <summary>
    /// Gets all active subscribers for a bird (for bird owner)
    /// </summary>
    Task<BirdWeeklySupportersDto> GetBirdSubscribersAsync(
        Guid birdId,
        Guid requestingUserId);

    /// <summary>
    /// Gets user's weekly support summary stats
    /// </summary>
    Task<UserWeeklySupportSummaryDto> GetUserSummaryAsync(Guid userId);

    /// <summary>
    /// Updates subscription settings (amount, schedule)
    /// </summary>
    Task<(WeeklySupportSubscriptionResponse? Response, ValidationErrorResponse? Error)> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Guid userId,
        UpdateWeeklySupportRequest request);

    /// <summary>
    /// Pauses a subscription (stops reminders)
    /// </summary>
    Task<bool> PauseSubscriptionAsync(Guid subscriptionId, Guid userId);

    /// <summary>
    /// Resumes a paused subscription
    /// </summary>
    Task<bool> ResumeSubscriptionAsync(Guid subscriptionId, Guid userId);

    /// <summary>
    /// Cancels a subscription permanently
    /// </summary>
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid userId);

    // =============================================
    // PAYMENT FLOW
    // =============================================

    /// <summary>
    /// Gets pending payment reminders waiting for user approval
    /// </summary>
    Task<List<WeeklyPaymentReminderDto>> GetPendingPaymentsAsync(Guid userId);

    /// <summary>
    /// Creates a pre-filled support intent for quick 1-click approval.
    /// Returns unsigned transaction for Phantom signing.
    /// </summary>
    Task<(ApproveWeeklyPaymentResponse? Response, ValidationErrorResponse? Error)> CreateQuickPaymentIntentAsync(
        Guid userId,
        ApproveWeeklyPaymentRequest request);

    /// <summary>
    /// Marks a weekly payment as completed after transaction confirms.
    /// Called by PaymentConfirmationJob when support intent completes.
    /// </summary>
    Task<bool> MarkPaymentCompletedAsync(Guid supportIntentId);

    /// <summary>
    /// Gets payment history for a subscription
    /// </summary>
    Task<List<WeeklyPaymentHistoryDto>> GetPaymentHistoryAsync(
        Guid subscriptionId,
        Guid userId,
        int limit = 20);

    // =============================================
    // BACKGROUND JOB METHODS
    // =============================================

    /// <summary>
    /// Processes due reminders for active subscriptions.
    /// Called by WeeklySupportReminderJob every 15 minutes.
    /// </summary>
    Task ProcessWeeklyRemindersAsync();

    /// <summary>
    /// Expires old payment reminders that weren't approved.
    /// Called by WeeklySupportReminderJob every hour.
    /// Increments consecutive_missed_count on subscription.
    /// Auto-pauses after 3 consecutive misses.
    /// </summary>
    Task ExpireOldPaymentRemindersAsync();
}
