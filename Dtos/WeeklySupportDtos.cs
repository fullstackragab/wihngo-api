using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// WEEKLY SUPPORT - Non-Custodial Recurring Donations
// =============================================
//
// User subscribes to weekly bird support.
// Each week: reminder -> 1-click approve -> Phantom signs -> bird funded.
// Non-custodial: Wihngo never holds user funds.
// =============================================

// =============================================
// SUBSCRIPTION MANAGEMENT
// =============================================

/// <summary>
/// Request to create a weekly support subscription
/// </summary>
public class CreateWeeklySupportRequest
{
    /// <summary>
    /// The bird to support weekly
    /// </summary>
    [Required(ErrorMessage = "Bird ID is required")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// Weekly amount to bird owner in USDC (default $1.00)
    /// </summary>
    [Range(0.10, 100, ErrorMessage = "Weekly amount must be between $0.10 and $100")]
    public decimal AmountUsdc { get; set; } = 1.00m;

    /// <summary>
    /// Optional Wihngo platform support per payment (default $0.00)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Wihngo support must be between $0 and $100")]
    public decimal WihngoSupportAmount { get; set; } = 0.00m;

    /// <summary>
    /// Day of week for reminders (0=Sunday, 6=Saturday)
    /// </summary>
    [Range(0, 6, ErrorMessage = "Day of week must be 0-6 (0=Sunday)")]
    public int DayOfWeek { get; set; } = 0;

    /// <summary>
    /// Preferred hour for reminder in UTC (0-23)
    /// </summary>
    [Range(0, 23, ErrorMessage = "Hour must be 0-23 (UTC)")]
    public int PreferredHour { get; set; } = 10;
}

/// <summary>
/// Request to update an existing subscription
/// </summary>
public class UpdateWeeklySupportRequest
{
    /// <summary>
    /// New weekly amount (optional)
    /// </summary>
    [Range(0.10, 100, ErrorMessage = "Weekly amount must be between $0.10 and $100")]
    public decimal? AmountUsdc { get; set; }

    /// <summary>
    /// New Wihngo support amount (optional)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Wihngo support must be between $0 and $100")]
    public decimal? WihngoSupportAmount { get; set; }

    /// <summary>
    /// New day of week (optional)
    /// </summary>
    [Range(0, 6)]
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// New preferred hour (optional)
    /// </summary>
    [Range(0, 23)]
    public int? PreferredHour { get; set; }
}

/// <summary>
/// Full subscription details response
/// </summary>
public class WeeklySupportSubscriptionResponse
{
    public Guid SubscriptionId { get; set; }

    // Bird info
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;
    public string? BirdImageUrl { get; set; }
    public string? BirdSpecies { get; set; }

    // Recipient info
    public Guid RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;

    // Amount details
    public decimal AmountUsdc { get; set; }
    public decimal WihngoSupportAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USDC";

    // Status
    public string Status { get; set; } = string.Empty;

    // Schedule
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public int PreferredHour { get; set; }
    public DateTime? NextReminderAt { get; set; }

    // Statistics
    public DateTime? LastPaymentAt { get; set; }
    public int TotalPaymentsCount { get; set; }
    public decimal TotalAmountPaid { get; set; }
    public int ConsecutiveMissedCount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? PausedAt { get; set; }
}

/// <summary>
/// Summary for listing subscriptions
/// </summary>
public class WeeklySupportSummaryDto
{
    public Guid SubscriptionId { get; set; }
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;
    public string? BirdImageUrl { get; set; }
    public decimal AmountUsdc { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? NextReminderAt { get; set; }
    public int TotalPaymentsCount { get; set; }
}

// =============================================
// PAYMENT REMINDERS & APPROVAL
// =============================================

/// <summary>
/// Pending weekly payment waiting for user approval
/// </summary>
public class WeeklyPaymentReminderDto
{
    public Guid PaymentId { get; set; }
    public Guid SubscriptionId { get; set; }

    // Bird info
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;
    public string? BirdImageUrl { get; set; }

    // Recipient info
    public string RecipientName { get; set; } = string.Empty;

    // Amount
    public decimal AmountUsdc { get; set; }
    public decimal WihngoSupportAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Week covered
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public DateTime? ReminderSentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }

    /// <summary>
    /// Deep link to approve this payment in mobile app
    /// </summary>
    public string ApproveDeepLink { get; set; } = string.Empty;
}

/// <summary>
/// Request to approve a weekly payment (creates support intent)
/// </summary>
public class ApproveWeeklyPaymentRequest
{
    /// <summary>
    /// The weekly payment ID to approve
    /// </summary>
    [Required(ErrorMessage = "Payment ID is required")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Idempotency key to prevent double-submission
    /// </summary>
    [StringLength(64)]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Response after approving - contains transaction for Phantom signing
/// </summary>
public class ApproveWeeklyPaymentResponse
{
    /// <summary>
    /// The support intent ID created
    /// </summary>
    public Guid IntentId { get; set; }

    /// <summary>
    /// The weekly payment ID
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Bird owner's wallet address
    /// </summary>
    public string BirdWalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Wihngo treasury wallet (if platform support > 0)
    /// </summary>
    public string? WihngoWalletAddress { get; set; }

    /// <summary>
    /// Amount going to bird owner
    /// </summary>
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Optional platform support
    /// </summary>
    public decimal WihngoSupportAmount { get; set; }

    /// <summary>
    /// Total amount to pay
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Base64 encoded unsigned Solana transaction for Phantom signing
    /// </summary>
    public string SerializedTransaction { get; set; } = string.Empty;

    /// <summary>
    /// When this intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this was already created (idempotent)
    /// </summary>
    public bool WasAlreadyCreated { get; set; }
}

// =============================================
// PAYMENT HISTORY
// =============================================

/// <summary>
/// Historical payment record
/// </summary>
public class WeeklyPaymentHistoryDto
{
    public Guid PaymentId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public decimal AmountUsdc { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================
// STATS
// =============================================

/// <summary>
/// Statistics for a bird's weekly supporters
/// </summary>
public class BirdWeeklySupportersDto
{
    public Guid BirdId { get; set; }
    public int ActiveSubscribers { get; set; }
    public decimal WeeklyAmountUsdc { get; set; }
    public decimal TotalReceivedUsdc { get; set; }
}

/// <summary>
/// User's weekly support summary
/// </summary>
public class UserWeeklySupportSummaryDto
{
    public int ActiveSubscriptions { get; set; }
    public int PausedSubscriptions { get; set; }
    public decimal WeeklyTotalUsdc { get; set; }
    public decimal LifetimeTotalPaidUsdc { get; set; }
    public int PendingApprovals { get; set; }
}
