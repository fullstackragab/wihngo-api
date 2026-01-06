using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Tracks each weekly payment for a subscription.
/// Created when a reminder is due, completed when user approves and transaction confirms.
/// </summary>
[Table("weekly_support_payments")]
public class WeeklySupportPayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The subscription this payment belongs to
    /// </summary>
    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Link to support intent when user approves payment
    /// </summary>
    [Column("support_intent_id")]
    public Guid? SupportIntentId { get; set; }

    /// <summary>
    /// Start of the week this payment covers (Monday)
    /// </summary>
    [Column("week_start_date")]
    public DateTime WeekStartDate { get; set; }

    /// <summary>
    /// End of the week this payment covers (Sunday)
    /// </summary>
    [Column("week_end_date")]
    public DateTime WeekEndDate { get; set; }

    /// <summary>
    /// Payment status: pending_reminder, reminder_sent, intent_created, completed, expired, skipped
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = WeeklyPaymentStatus.PendingReminder;

    /// <summary>
    /// Amount to bird owner (copied from subscription)
    /// </summary>
    [Column("amount_usdc")]
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Optional platform support (copied from subscription)
    /// </summary>
    [Column("wihngo_support_amount")]
    public decimal WihngoSupportAmount { get; set; }

    /// <summary>
    /// When the reminder notification was sent
    /// </summary>
    [Column("reminder_sent_at")]
    public DateTime? ReminderSentAt { get; set; }

    /// <summary>
    /// Whether push notification was sent
    /// </summary>
    [Column("reminder_push_sent")]
    public bool ReminderPushSent { get; set; }

    /// <summary>
    /// Whether email notification was sent
    /// </summary>
    [Column("reminder_email_sent")]
    public bool ReminderEmailSent { get; set; }

    /// <summary>
    /// When the support intent was created (user clicked approve)
    /// </summary>
    [Column("intent_created_at")]
    public DateTime? IntentCreatedAt { get; set; }

    /// <summary>
    /// When this payment reminder expires (7 days after reminder sent)
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the payment was confirmed on-chain
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    [Column("error_message")]
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total payment amount
    /// </summary>
    [NotMapped]
    public decimal TotalAmount => AmountUsdc + WihngoSupportAmount;

    /// <summary>
    /// Whether payment has expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Whether payment can still be approved
    /// </summary>
    [NotMapped]
    public bool CanBeApproved =>
        Status is WeeklyPaymentStatus.ReminderSent or WeeklyPaymentStatus.IntentCreated
        && !IsExpired;
}

/// <summary>
/// Weekly payment status constants
/// </summary>
public static class WeeklyPaymentStatus
{
    /// <summary>
    /// Payment record created, waiting for reminder time
    /// </summary>
    public const string PendingReminder = "pending_reminder";

    /// <summary>
    /// Reminder sent to user (push/email)
    /// </summary>
    public const string ReminderSent = "reminder_sent";

    /// <summary>
    /// User clicked approve, SupportIntent created
    /// </summary>
    public const string IntentCreated = "intent_created";

    /// <summary>
    /// Payment completed and confirmed on-chain
    /// </summary>
    public const string Completed = "completed";

    /// <summary>
    /// Payment expired (user didn't approve in time)
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// Payment skipped (e.g., recipient has no wallet)
    /// </summary>
    public const string Skipped = "skipped";
}
