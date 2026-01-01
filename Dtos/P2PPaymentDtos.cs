using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// PREFLIGHT PAYMENT
// =============================================

/// <summary>
/// Request to validate a payment before creating intent
/// </summary>
public class PreflightPaymentRequest
{
    /// <summary>
    /// Recipient user ID or username
    /// </summary>
    [Required]
    public string RecipientId { get; set; } = string.Empty;

    /// <summary>
    /// Amount in USD (will be converted to USDC 1:1)
    /// </summary>
    [Required]
    [Range(0.01, 10000)]
    public decimal Amount { get; set; }
}

/// <summary>
/// Response from preflight validation
/// </summary>
public class PreflightPaymentResponse
{
    public bool Valid { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientUsername { get; set; }
    public Guid? RecipientUserId { get; set; }
    public bool GasNeeded { get; set; }
    public decimal EstimatedFee { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

// =============================================
// CREATE PAYMENT INTENT
// =============================================

/// <summary>
/// Request to create a new payment intent
/// </summary>
public class CreatePaymentIntentRequest
{
    /// <summary>
    /// Recipient user ID
    /// </summary>
    [Required]
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Amount in USDC to send
    /// </summary>
    [Required]
    [Range(0.01, 10000)]
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Optional memo for the payment
    /// </summary>
    [MaxLength(255)]
    public string? Memo { get; set; }
}

/// <summary>
/// Response from creating a payment intent
/// </summary>
public class PaymentIntentResponse
{
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Base64 encoded unsigned Solana transaction
    /// </summary>
    public string SerializedTransaction { get; set; } = string.Empty;

    /// <summary>
    /// Whether the platform is sponsoring gas fees
    /// </summary>
    public bool GasSponsored { get; set; }

    /// <summary>
    /// Fee in USDC (0 if not sponsored, 0.01 if sponsored)
    /// </summary>
    public decimal FeeUsdc { get; set; }

    /// <summary>
    /// Total amount sender will pay (amount + fee)
    /// </summary>
    public decimal TotalUsdc { get; set; }

    /// <summary>
    /// Amount recipient will receive
    /// </summary>
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// When this intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Recipient info for display
    /// </summary>
    public RecipientInfo? Recipient { get; set; }
}

public class RecipientInfo
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? ProfileImage { get; set; }
    public string? WalletAddress { get; set; }
}

// =============================================
// SUBMIT SIGNED TRANSACTION
// =============================================

/// <summary>
/// Request to submit a signed transaction
/// </summary>
public class SubmitTransactionRequest
{
    /// <summary>
    /// Payment intent ID
    /// </summary>
    [Required]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Base64 encoded signed transaction from Phantom
    /// </summary>
    [Required]
    public string SignedTransaction { get; set; } = string.Empty;

    /// <summary>
    /// Optional idempotency key to prevent duplicate submissions.
    /// Should be unique per submission attempt (e.g., "{paymentId}-{attemptNumber}-{timestamp}").
    /// If a request with the same idempotency key was already processed, the original result is returned.
    /// </summary>
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Idempotency key must be 1-64 characters")]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Response from submitting a transaction
/// </summary>
public class SubmitTransactionResponse
{
    public Guid PaymentId { get; set; }
    public string? SolanaSignature { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// True if this response is from a previous submission with the same idempotency key
    /// </summary>
    public bool WasAlreadySubmitted { get; set; }
}

// =============================================
// PAYMENT STATUS
// =============================================

/// <summary>
/// Detailed payment status response
/// </summary>
public class PaymentStatusResponse
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SolanaSignature { get; set; }
    public int Confirmations { get; set; }
    public int RequiredConfirmations { get; set; } = 32;

    public decimal AmountUsdc { get; set; }
    public decimal FeeUsdc { get; set; }
    public decimal TotalUsdc { get; set; }
    public bool GasSponsored { get; set; }
    public string? Memo { get; set; }

    public RecipientInfo? Recipient { get; set; }
    public SenderInfo? Sender { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class SenderInfo
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? ProfileImage { get; set; }
}

// =============================================
// PAYMENT HISTORY
// =============================================

/// <summary>
/// Summary of a payment for list views
/// </summary>
public class PaymentSummary
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AmountUsdc { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the current user is the sender
    /// </summary>
    public bool IsSender { get; set; }

    /// <summary>
    /// The other party in the transaction
    /// </summary>
    public UserSummaryDto? OtherParty { get; set; }
}
