using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// SUPPORT INTENT - For bird donations (USDC on Solana)
// =============================================

/// <summary>
/// Request to create a support intent for a bird.
/// MVP: Requires connected wallet with sufficient USDC + SOL.
/// </summary>
public class CreateSupportIntentRequest
{
    /// <summary>
    /// The bird to support
    /// </summary>
    [Required(ErrorMessage = "Bird ID is required")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// Amount in USDC to give to the bird owner
    /// </summary>
    [Required(ErrorMessage = "Support amount is required")]
    [Range(0.01, 10000, ErrorMessage = "Support amount must be between $0.01 and $10,000")]
    public decimal SupportAmount { get; set; }

    /// <summary>
    /// Whether to include platform fee (5% of support amount).
    /// Default: true. Set to false to send 100% to bird owner.
    /// </summary>
    public bool IncludePlatformFee { get; set; } = true;

    /// <summary>
    /// Currency - MVP supports USDC only
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [RegularExpression("^(USDC)$", ErrorMessage = "MVP only supports USDC")]
    public string Currency { get; set; } = "USDC";
}

/// <summary>
/// Response from creating a support intent
/// </summary>
public class SupportIntentResponse
{
    public Guid IntentId { get; set; }
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;
    public Guid RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientWalletAddress { get; set; }

    /// <summary>
    /// Amount going to bird owner (USDC)
    /// </summary>
    public decimal SupportAmount { get; set; }

    /// <summary>
    /// Platform fee (5% of support amount, USDC)
    /// </summary>
    public decimal PlatformFee { get; set; }

    /// <summary>
    /// Platform fee percentage (e.g., 5 for 5%)
    /// </summary>
    public decimal PlatformFeePercent { get; set; }

    /// <summary>
    /// Total USDC user will pay (supportAmount + platformFee + gasFeeUsdc)
    /// </summary>
    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "USDC";

    /// <summary>
    /// Status: pending, awaiting_signature, submitted, confirming, completed, expired, cancelled, failed
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Base64 encoded unsigned Solana transaction for the user to sign
    /// </summary>
    public string? SerializedTransaction { get; set; }

    /// <summary>
    /// Whether gas fees are sponsored by platform
    /// </summary>
    public bool GasSponsored { get; set; }

    /// <summary>
    /// Gas sponsorship fee in USDC (if sponsored)
    /// </summary>
    public decimal GasFeeUsdc { get; set; }

    /// <summary>
    /// When this intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

// =============================================
// BALANCE CHECK - Before creating intent
// =============================================

/// <summary>
/// Request to check if user can support with given amount
/// </summary>
public class CheckSupportBalanceRequest
{
    [Required]
    public Guid BirdId { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal SupportAmount { get; set; }

    /// <summary>
    /// Whether to include platform fee (5% of support amount)
    /// </summary>
    public bool IncludePlatformFee { get; set; } = true;
}

/// <summary>
/// Response from balance check
/// </summary>
public class CheckSupportBalanceResponse
{
    /// <summary>
    /// Whether user can proceed with support
    /// </summary>
    public bool CanSupport { get; set; }

    /// <summary>
    /// Whether user has a connected wallet
    /// </summary>
    public bool HasWallet { get; set; }

    /// <summary>
    /// User's current USDC balance
    /// </summary>
    public decimal UsdcBalance { get; set; }

    /// <summary>
    /// User's current SOL balance
    /// </summary>
    public decimal SolBalance { get; set; }

    /// <summary>
    /// Amount going to bird owner
    /// </summary>
    public decimal SupportAmount { get; set; }

    /// <summary>
    /// Platform fee (5% of support amount)
    /// </summary>
    public decimal PlatformFee { get; set; }

    /// <summary>
    /// Platform fee percentage (e.g., 5 for 5%)
    /// </summary>
    public decimal PlatformFeePercent { get; set; }

    /// <summary>
    /// Total USDC required (support + platformFee + gasFee)
    /// </summary>
    public decimal UsdcRequired { get; set; }

    /// <summary>
    /// Minimum SOL required for gas (if not sponsored)
    /// </summary>
    public decimal SolRequired { get; set; }

    /// <summary>
    /// Whether platform will sponsor gas fees
    /// </summary>
    public bool GasSponsored { get; set; }

    /// <summary>
    /// Gas sponsorship fee in USDC
    /// </summary>
    public decimal GasFeeUsdc { get; set; }

    /// <summary>
    /// Error code if cannot support
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Recipient info
    /// </summary>
    public RecipientInfo? Recipient { get; set; }

    /// <summary>
    /// Bird info
    /// </summary>
    public BirdSupportInfo? Bird { get; set; }
}

public class BirdSupportInfo
{
    public Guid BirdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

// =============================================
// VALIDATION ERROR RESPONSE
// =============================================

/// <summary>
/// Structured validation error response
/// </summary>
public class ValidationErrorResponse
{
    public bool Success { get; set; } = false;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> FieldErrors { get; set; } = new();
}

/// <summary>
/// Error codes for support intents
/// </summary>
public static class SupportIntentErrorCodes
{
    public const string BirdNotFound = "BIRD_NOT_FOUND";
    public const string BirdInactive = "BIRD_INACTIVE";
    public const string CannotSupportOwnBird = "CANNOT_SUPPORT_OWN_BIRD";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string InvalidCurrency = "INVALID_CURRENCY";

    // Wallet errors
    public const string WalletRequired = "WALLET_REQUIRED";
    public const string InsufficientUsdc = "INSUFFICIENT_USDC";
    public const string InsufficientSol = "INSUFFICIENT_SOL";
    public const string RecipientNoWallet = "RECIPIENT_NO_WALLET";

    // Intent errors
    public const string IntentExpired = "INTENT_EXPIRED";
    public const string IntentNotFound = "INTENT_NOT_FOUND";
    public const string IntentAlreadyProcessed = "INTENT_ALREADY_PROCESSED";

    public const string TransactionFailed = "TRANSACTION_FAILED";
    public const string InternalError = "INTERNAL_ERROR";
}
