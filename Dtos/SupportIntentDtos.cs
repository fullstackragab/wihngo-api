using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// SUPPORT INTENT - Bird-First Payment Model
// =============================================
//
// Core Principles:
// 1. 100% of bird amount goes to bird owner (NEVER deducted)
// 2. Wihngo support is OPTIONAL and ADDITIVE (not a percentage)
// 3. Minimum Wihngo support: $0.05 (if > 0)
// 4. Two separate on-chain transfers
// =============================================

/// <summary>
/// Request to create a support intent for a bird.
/// Bird money is sacred - 100% goes to bird owner.
/// </summary>
public class CreateSupportIntentRequest
{
    /// <summary>
    /// The bird to support
    /// </summary>
    [Required(ErrorMessage = "Bird ID is required")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// Amount in USDC to give to the bird owner (100% - never deducted from)
    /// </summary>
    [Required(ErrorMessage = "Bird amount is required")]
    [Range(0.01, 10000, ErrorMessage = "Bird amount must be between $0.01 and $10,000")]
    public decimal BirdAmount { get; set; }

    /// <summary>
    /// Optional support for Wihngo (additive, not deducted from bird amount).
    /// Default: $0.05 (suggested). Minimum: $0.05 if provided.
    /// Set to 0 to skip Wihngo support entirely.
    /// </summary>
    [Range(0, 10000, ErrorMessage = "Wihngo support must be between $0 and $10,000")]
    public decimal WihngoSupportAmount { get; set; } = 0.05m;

    /// <summary>
    /// Currency - only USDC on Solana supported
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [RegularExpression("^(USDC)$", ErrorMessage = "Only USDC is supported")]
    public string Currency { get; set; } = "USDC";

    /// <summary>
    /// Optional idempotency key for preventing duplicate intent creation.
    /// Frontend generates as SHA-256 hash of "{userId}|{birdId}|{birdAmount}|{wihngoAmount}|{minuteBucket}".
    /// If a request with the same key was already processed within 1 minute, the existing intent is returned.
    /// </summary>
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Idempotency key must be 1-64 characters")]
    public string? IdempotencyKey { get; set; }
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

    /// <summary>
    /// Bird owner's wallet address (receives 100% of bird amount)
    /// </summary>
    public string? BirdWalletAddress { get; set; }

    /// <summary>
    /// Wihngo treasury wallet (receives optional support)
    /// </summary>
    public string? WihngoWalletAddress { get; set; }

    /// <summary>
    /// Amount going to bird owner (100% - never deducted from)
    /// </summary>
    public decimal BirdAmount { get; set; }

    /// <summary>
    /// Optional support for Wihngo (additive)
    /// </summary>
    public decimal WihngoSupportAmount { get; set; }

    /// <summary>
    /// Total USDC user will pay (birdAmount + wihngoSupportAmount)
    /// </summary>
    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "USDC";

    /// <summary>
    /// USDC mint address on Solana
    /// </summary>
    public string UsdcMintAddress { get; set; } = string.Empty;

    /// <summary>
    /// Status: pending, awaiting_signature, submitted, confirming, completed, expired, cancelled, failed
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Base64 encoded unsigned Solana transaction for the user to sign.
    /// Contains both transfers (bird + wihngo if applicable).
    /// </summary>
    public string? SerializedTransaction { get; set; }

    /// <summary>
    /// When this intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

// =============================================
// PREFLIGHT CHECK - Before creating intent
// =============================================

/// <summary>
/// Request to check if user can support a bird with given amounts
/// </summary>
public class SupportPreflightRequest
{
    [Required]
    public Guid BirdId { get; set; }

    /// <summary>
    /// Amount going to bird owner (100%)
    /// </summary>
    [Required]
    [Range(0.01, 10000)]
    public decimal BirdAmount { get; set; }

    /// <summary>
    /// Optional support for Wihngo. Minimum $0.05 if > 0.
    /// </summary>
    [Range(0, 10000)]
    public decimal WihngoSupportAmount { get; set; } = 0.05m;
}

/// <summary>
/// Response from preflight check
/// </summary>
public class SupportPreflightResponse
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
    /// User's current SOL balance (for gas)
    /// </summary>
    public decimal SolBalance { get; set; }

    /// <summary>
    /// Amount going to bird owner (100%)
    /// </summary>
    public decimal BirdAmount { get; set; }

    /// <summary>
    /// Optional support for Wihngo
    /// </summary>
    public decimal WihngoSupportAmount { get; set; }

    /// <summary>
    /// Total USDC required (birdAmount + wihngoSupportAmount)
    /// </summary>
    public decimal TotalUsdcRequired { get; set; }

    /// <summary>
    /// Minimum SOL required for gas
    /// </summary>
    public decimal SolRequired { get; set; }

    /// <summary>
    /// Error code if cannot support
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Recipient (bird owner) info
    /// </summary>
    public RecipientInfo? Recipient { get; set; }

    /// <summary>
    /// Bird info
    /// </summary>
    public BirdSupportInfo? Bird { get; set; }

    /// <summary>
    /// USDC mint address on Solana
    /// </summary>
    public string UsdcMintAddress { get; set; } = string.Empty;

    /// <summary>
    /// Wihngo treasury wallet address
    /// </summary>
    public string WihngoWalletAddress { get; set; } = string.Empty;
}

public class BirdSupportInfo
{
    public Guid BirdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

// Note: RecipientInfo is defined in P2PPaymentDtos.cs

// =============================================
// INDEPENDENT WIHNGO SUPPORT
// =============================================

/// <summary>
/// Request to support Wihngo independently (no bird involved)
/// </summary>
public class SupportWihngoRequest
{
    /// <summary>
    /// Amount in USDC to support Wihngo
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.05, 10000, ErrorMessage = "Support amount must be between $0.05 and $10,000")]
    public decimal Amount { get; set; }
}

/// <summary>
/// Response from creating Wihngo support intent
/// </summary>
public class SupportWihngoResponse
{
    public Guid IntentId { get; set; }
    public decimal Amount { get; set; }
    public string WihngoWalletAddress { get; set; } = string.Empty;
    public string UsdcMintAddress { get; set; } = string.Empty;
    public string? SerializedTransaction { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================
// TRANSACTION CONFIRMATION
// =============================================

/// <summary>
/// Request to confirm a support transaction
/// </summary>
public class ConfirmSupportTransactionRequest
{
    /// <summary>
    /// Support intent ID
    /// </summary>
    [Required]
    public Guid IntentId { get; set; }

    /// <summary>
    /// Signed transactions from Phantom wallet
    /// </summary>
    [Required]
    public List<TransactionSignature> Transactions { get; set; } = new();
}

/// <summary>
/// A transaction signature with its type
/// </summary>
public class TransactionSignature
{
    /// <summary>
    /// Type: BIRD or WIHNGO
    /// </summary>
    [Required]
    [RegularExpression("^(BIRD|WIHNGO)$", ErrorMessage = "Type must be BIRD or WIHNGO")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Solana transaction signature
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
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
    public const string SupportNotEnabled = "SUPPORT_NOT_ENABLED";
    public const string CannotSupportOwnBird = "CANNOT_SUPPORT_OWN_BIRD";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string InvalidCurrency = "INVALID_CURRENCY";
    public const string WihngoSupportTooLow = "WIHNGO_SUPPORT_TOO_LOW";

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

// =============================================
// TYPE ALIASES FOR BACKWARDS COMPATIBILITY
// =============================================

/// <summary>
/// Alias for SupportPreflightRequest (used by P2PPaymentsController)
/// </summary>
public class CheckSupportBalanceRequest : SupportPreflightRequest { }

/// <summary>
/// Alias for SupportPreflightResponse (used by P2PPaymentsController)
/// </summary>
public class CheckSupportBalanceResponse : SupportPreflightResponse { }

// =============================================
// EXTERNAL TRANSACTION VERIFICATION
// =============================================
// For verifying externally-submitted Solana transactions
// containing two USDC transfers (bird owner + Wihngo)
// =============================================

/// <summary>
/// Request to verify an externally-submitted Solana transaction.
/// The transaction must contain exactly two USDC SPL token transfers:
/// one to the bird owner and one to the Wihngo platform.
/// </summary>
public class VerifySolanaSupportRequest
{
    /// <summary>
    /// Solana transaction signature/hash to verify
    /// </summary>
    [Required(ErrorMessage = "Transaction hash is required")]
    [MinLength(32, ErrorMessage = "Transaction hash must be at least 32 characters")]
    [MaxLength(128, ErrorMessage = "Transaction hash must not exceed 128 characters")]
    public string TxHash { get; set; } = string.Empty;

    /// <summary>
    /// The bird being supported
    /// </summary>
    [Required(ErrorMessage = "Bird ID is required")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// Bird owner's wallet public key (destination for bird amount)
    /// </summary>
    [Required(ErrorMessage = "Bird wallet is required")]
    public string BirdWallet { get; set; } = string.Empty;

    /// <summary>
    /// Expected amount to bird owner in cents (e.g., 500 = $5.00)
    /// USDC on Solana has 6 decimals, so 500 cents = 5.00 USDC = 5,000,000 raw
    /// </summary>
    [Required(ErrorMessage = "Bird amount is required")]
    [Range(1, 1000000, ErrorMessage = "Bird amount must be between 1 and 1,000,000 cents")]
    public int BirdAmountCents { get; set; }

    /// <summary>
    /// Expected amount to Wihngo in cents (e.g., 100 = $1.00)
    /// </summary>
    [Required(ErrorMessage = "Wihngo amount is required")]
    [Range(1, 1000000, ErrorMessage = "Wihngo amount must be between 1 and 1,000,000 cents")]
    public int WihngoAmountCents { get; set; }
}

/// <summary>
/// Response from Solana transaction verification
/// </summary>
public class VerifySolanaSupportResponse
{
    /// <summary>
    /// Whether the verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status: pending, confirmed, failed
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// The transaction hash that was verified
    /// </summary>
    public string TxHash { get; set; } = string.Empty;

    /// <summary>
    /// Bird support payment ID (if successfully created/found)
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Bird ID that was supported
    /// </summary>
    public Guid? BirdId { get; set; }

    /// <summary>
    /// Bird name
    /// </summary>
    public string? BirdName { get; set; }

    /// <summary>
    /// Verified payer wallet address
    /// </summary>
    public string? PayerWallet { get; set; }

    /// <summary>
    /// Bird amount in cents (verified)
    /// </summary>
    public int BirdAmountCents { get; set; }

    /// <summary>
    /// Wihngo amount in cents (verified)
    /// </summary>
    public int WihngoAmountCents { get; set; }

    /// <summary>
    /// Error message if verification failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Detailed verification results
    /// </summary>
    public VerificationDetails? Details { get; set; }
}

/// <summary>
/// Detailed verification breakdown
/// </summary>
public class VerificationDetails
{
    public bool TransactionFound { get; set; }
    public bool TransactionSucceeded { get; set; }
    public bool MintMatches { get; set; }
    public bool PayerMatches { get; set; }
    public int UsdcTransferCount { get; set; }
    public bool BirdTransferValid { get; set; }
    public bool WihngoTransferValid { get; set; }
    public decimal ActualBirdAmount { get; set; }
    public decimal ActualWihngoAmount { get; set; }
}
