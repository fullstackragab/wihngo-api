using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// WALLET CONNECT INTENT - Android Browser-Switch Solution
// =============================================
//
// Solves the problem where Phantom's redirect lands in a different browser,
// losing the user's JWT session. Uses stateless intent pattern with
// state token for recovery.
//
// Flow:
// 1. POST /api/wallet-connect/intents → Get state + nonce
// 2. Redirect to Phantom with state parameter
// 3. Phantom redirects to callback with state + publicKey + signature
// 4. POST /api/wallet-connect/callback → Validate & link wallet
// =============================================

/// <summary>
/// Request to create a wallet connection intent
/// </summary>
public class CreateWalletConnectIntentRequest
{
    /// <summary>
    /// Purpose of the connection: connect, sign, transaction, support, payment
    /// </summary>
    [MaxLength(50)]
    public string Purpose { get; set; } = "connect";

    /// <summary>
    /// Wallet provider (phantom, solflare, etc.)
    /// </summary>
    [MaxLength(50)]
    public string WalletProvider { get; set; } = "phantom";

    /// <summary>
    /// Optional redirect URL after successful connection.
    /// If not provided, uses default app URL.
    /// </summary>
    [MaxLength(500)]
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Optional metadata to store with the intent (JSON).
    /// Can be used to pass client-specific data through the flow.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response from creating a wallet connection intent
/// </summary>
public class WalletConnectIntentResponse
{
    /// <summary>
    /// Intent ID (for reference)
    /// </summary>
    public Guid IntentId { get; set; }

    /// <summary>
    /// State token to pass to wallet (CSRF protection)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Nonce (message) for the user to sign
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Full message to display/sign (includes nonce and context)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Callback URL to configure in wallet redirect
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// When this intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the intent was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to complete a wallet connection (callback from Phantom)
/// </summary>
public class WalletConnectCallbackRequest
{
    /// <summary>
    /// State token (must match the original intent)
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [MaxLength(64)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Wallet public key from Phantom
    /// </summary>
    [Required(ErrorMessage = "Public key is required")]
    [MaxLength(44)]
    [RegularExpression(@"^[1-9A-HJ-NP-Za-km-z]{32,44}$", ErrorMessage = "Invalid Solana public key format")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Signature of the nonce message
    /// </summary>
    [Required(ErrorMessage = "Signature is required")]
    [MaxLength(128)]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Optional Phantom session token (for encrypted communication)
    /// </summary>
    [MaxLength(256)]
    public string? PhantomSession { get; set; }
}

/// <summary>
/// Response from wallet connect callback
/// </summary>
public class WalletConnectCallbackResponse
{
    /// <summary>
    /// Whether the connection was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The linked wallet ID (if successful)
    /// </summary>
    public Guid? WalletId { get; set; }

    /// <summary>
    /// The wallet public key
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    /// Whether this is a new wallet (vs already linked)
    /// </summary>
    public bool IsNewWallet { get; set; }

    /// <summary>
    /// User ID the wallet is linked to
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional: new JWT token for session recovery
    /// Only provided if user was authenticated when creating intent
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Optional: refresh token
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Redirect URL to navigate to after success
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Original metadata from the intent (for client state recovery)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request to get intent status (for polling)
/// </summary>
public class GetWalletConnectIntentRequest
{
    /// <summary>
    /// Intent ID or state token
    /// </summary>
    [Required]
    public string IntentIdOrState { get; set; } = string.Empty;
}

/// <summary>
/// Response with intent status
/// </summary>
public class WalletConnectIntentStatusResponse
{
    public Guid IntentId { get; set; }

    /// <summary>
    /// Status: pending, awaiting_callback, processing, completed, expired, cancelled, failed
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Wallet public key (if completed)
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    /// Whether the intent is still valid (not expired/cancelled)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the intent expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the intent was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Request to check for pending wallet intents (recovery flow)
/// </summary>
public class CheckPendingIntentRequest
{
    /// <summary>
    /// Optional: check by wallet public key
    /// </summary>
    [MaxLength(44)]
    public string? PublicKey { get; set; }
}

/// <summary>
/// Response with pending intent info
/// </summary>
public class PendingWalletIntentResponse
{
    /// <summary>
    /// Whether there's a pending intent
    /// </summary>
    public bool HasPendingIntent { get; set; }

    /// <summary>
    /// The pending intent details (if any)
    /// </summary>
    public WalletConnectIntentStatusResponse? Intent { get; set; }

    /// <summary>
    /// Message about what to do next
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Error codes for wallet connect intents
/// </summary>
public static class WalletConnectErrorCodes
{
    // Intent errors
    public const string IntentNotFound = "INTENT_NOT_FOUND";
    public const string IntentExpired = "INTENT_EXPIRED";
    public const string IntentAlreadyUsed = "INTENT_ALREADY_USED";
    public const string IntentCancelled = "INTENT_CANCELLED";
    public const string InvalidState = "INVALID_STATE";

    // Validation errors
    public const string InvalidPublicKey = "INVALID_PUBLIC_KEY";
    public const string InvalidSignature = "INVALID_SIGNATURE";
    public const string SignatureVerificationFailed = "SIGNATURE_VERIFICATION_FAILED";

    // Wallet errors
    public const string WalletAlreadyLinked = "WALLET_ALREADY_LINKED";
    public const string WalletLinkFailed = "WALLET_LINK_FAILED";

    // Auth errors
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string AuthRequired = "AUTH_REQUIRED";

    // General errors
    public const string InternalError = "INTERNAL_ERROR";
    public const string RateLimited = "RATE_LIMITED";
}
