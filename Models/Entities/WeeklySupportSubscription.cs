using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents a user's subscription to provide weekly support to a bird.
/// Non-custodial: User receives reminders and approves each payment via Phantom wallet.
/// </summary>
[Table("weekly_support_subscriptions")]
public class WeeklySupportSubscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user providing weekly support
    /// </summary>
    [Column("subscriber_user_id")]
    public Guid SubscriberUserId { get; set; }

    /// <summary>
    /// The bird being supported
    /// </summary>
    [Column("bird_id")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// The bird owner receiving funds
    /// </summary>
    [Column("recipient_user_id")]
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Weekly amount to bird owner (default $1.00 USDC)
    /// </summary>
    [Column("amount_usdc")]
    public decimal AmountUsdc { get; set; } = 1.00m;

    /// <summary>
    /// Optional platform support amount (default $0.00)
    /// </summary>
    [Column("wihngo_support_amount")]
    public decimal WihngoSupportAmount { get; set; } = 0.00m;

    [Column("currency")]
    public string Currency { get; set; } = "USDC";

    /// <summary>
    /// Subscription status: active, paused, cancelled
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = WeeklySupportStatus.Active;

    /// <summary>
    /// Day of week for reminders (0=Sunday, 6=Saturday)
    /// </summary>
    [Column("day_of_week")]
    public int DayOfWeek { get; set; } = 0;

    /// <summary>
    /// Preferred hour for reminder (0-23 UTC)
    /// </summary>
    [Column("preferred_hour")]
    public int PreferredHour { get; set; } = 10;

    /// <summary>
    /// When the next reminder should be sent
    /// </summary>
    [Column("next_reminder_at")]
    public DateTime? NextReminderAt { get; set; }

    /// <summary>
    /// When the last successful payment was made
    /// </summary>
    [Column("last_payment_at")]
    public DateTime? LastPaymentAt { get; set; }

    /// <summary>
    /// Total number of successful payments
    /// </summary>
    [Column("total_payments_count")]
    public int TotalPaymentsCount { get; set; }

    /// <summary>
    /// Total amount paid over lifetime of subscription
    /// </summary>
    [Column("total_amount_paid")]
    public decimal TotalAmountPaid { get; set; }

    /// <summary>
    /// Number of consecutive missed payments (resets on success)
    /// </summary>
    [Column("consecutive_missed_count")]
    public int ConsecutiveMissedCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("paused_at")]
    public DateTime? PausedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Total amount per payment (bird + optional platform support)
    /// </summary>
    [NotMapped]
    public decimal TotalAmount => AmountUsdc + WihngoSupportAmount;

    /// <summary>
    /// Whether the subscription is currently active
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == WeeklySupportStatus.Active;
}

/// <summary>
/// Weekly support subscription status constants
/// </summary>
public static class WeeklySupportStatus
{
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Cancelled = "cancelled";
}
