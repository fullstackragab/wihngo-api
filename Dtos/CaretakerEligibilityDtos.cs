using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

/// <summary>
/// Response for GET /api/support/eligibility - returns caretaker's weekly cap status
/// </summary>
public class CaretakerEligibilityResponse
{
    /// <summary>
    /// The caretaker's user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Maximum USDC this caretaker can receive per week in baseline support
    /// </summary>
    public decimal WeeklyCap { get; set; }

    /// <summary>
    /// Total baseline support received this week (does NOT include gifts)
    /// </summary>
    public decimal ReceivedThisWeek { get; set; }

    /// <summary>
    /// Remaining baseline support allowance for this week
    /// Formula: WeeklyCap - ReceivedThisWeek
    /// </summary>
    public decimal Remaining { get; set; }

    /// <summary>
    /// Current week identifier (ISO week: YYYY-WW)
    /// </summary>
    public string WeekId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the caretaker can receive baseline support (remaining > 0)
    /// </summary>
    public bool CanReceiveBaseline { get; set; }

    /// <summary>
    /// Whether the caretaker has a wallet linked to receive support
    /// </summary>
    public bool HasWallet { get; set; }

    /// <summary>
    /// Caretaker's wallet address (if linked)
    /// </summary>
    public string? WalletAddress { get; set; }

    /// <summary>
    /// Caretaker's display name
    /// </summary>
    public string? CaretakerName { get; set; }

    /// <summary>
    /// Error code if the request failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to record a confirmed support transaction
/// Called AFTER the frontend verifies the transaction on Solana
/// </summary>
public class RecordSupportRequest
{
    /// <summary>
    /// Solana transaction signature (required for on-chain verification)
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string TxSignature { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type: "baseline" or "gift"
    /// If not specified, backend will auto-classify based on remaining allowance
    /// </summary>
    [MaxLength(20)]
    public string? Type { get; set; }

    /// <summary>
    /// Amount in USDC
    /// </summary>
    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    /// <summary>
    /// The caretaker (recipient) user ID
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Optional bird reference (for tracking only, does not affect payout)
    /// </summary>
    public Guid? BirdId { get; set; }

    /// <summary>
    /// Optional idempotency key to prevent duplicate records
    /// </summary>
    [MaxLength(64)]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Response for POST /api/support/record
/// </summary>
public class RecordSupportResponse
{
    /// <summary>
    /// Whether the transaction was successfully recorded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The generated receipt ID
    /// </summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>
    /// Final transaction type that was recorded ("baseline" or "gift")
    /// May differ from request if auto-classified
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the transaction was verified on-chain
    /// </summary>
    public bool VerifiedOnChain { get; set; }

    /// <summary>
    /// Updated eligibility info for the caretaker
    /// </summary>
    public CaretakerEligibilityResponse? UpdatedEligibility { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this was a duplicate submission (idempotent return)
    /// </summary>
    public bool WasAlreadyRecorded { get; set; }
}

/// <summary>
/// Request to send a one-time gift (bypasses weekly cap)
/// </summary>
public class SendGiftRequest
{
    /// <summary>
    /// The caretaker (recipient) user ID
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Amount in USDC
    /// </summary>
    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional bird reference (for display only)
    /// </summary>
    public Guid? BirdId { get; set; }

    /// <summary>
    /// Idempotency key for preventing duplicates
    /// </summary>
    [MaxLength(64)]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Error codes for caretaker eligibility operations
/// </summary>
public static class CaretakerEligibilityErrorCodes
{
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string NoWallet = "NO_WALLET";
    public const string WeeklyCapReached = "WEEKLY_CAP_REACHED";
    public const string InvalidTransaction = "INVALID_TRANSACTION";
    public const string TransactionNotVerified = "TX_NOT_VERIFIED";
    public const string DuplicateTransaction = "DUPLICATE_TX";
    public const string AmountMismatch = "AMOUNT_MISMATCH";
    public const string WalletMismatch = "WALLET_MISMATCH";
    public const string InvalidMint = "INVALID_MINT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string CannotSupportSelf = "CANNOT_SUPPORT_SELF";
    public const string MaxBirdsReached = "MAX_BIRDS_REACHED";
}
