using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents an intent to connect a wallet.
///
/// This entity solves the Android browser-switch problem:
/// - When Phantom redirects back after signing, Android may open a different browser
/// - The user loses their JWT session in the new browser
/// - This intent provides a stateless recovery mechanism via the callback
///
/// Flow:
/// 1. User clicks "Connect Wallet" → Backend creates intent with state token
/// 2. Frontend redirects to Phantom with state parameter
/// 3. Phantom signs and redirects to callback URL
/// 4. Callback page (possibly in different browser) validates intent + signature
/// 5. Backend links wallet and returns session/continuation token
/// </summary>
[Table("wallet_connect_intents")]
public class WalletConnectIntent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// User who initiated the connection (nullable for anonymous flows)
    /// </summary>
    [Column("user_id")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Random state token for CSRF protection and intent matching.
    /// This is passed to Phantom and returned in the callback.
    /// </summary>
    [Required]
    [MaxLength(64)]
    [Column("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Nonce for signature verification (message the user signs)
    /// </summary>
    [Required]
    [MaxLength(128)]
    [Column("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Purpose of the connection: "connect", "sign", "transaction", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("purpose")]
    public string Purpose { get; set; } = WalletConnectPurpose.Connect;

    /// <summary>
    /// Status of the intent
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = WalletConnectIntentStatus.Pending;

    /// <summary>
    /// Wallet public key (set after successful callback)
    /// </summary>
    [MaxLength(44)]
    [Column("public_key")]
    public string? PublicKey { get; set; }

    /// <summary>
    /// Wallet provider (phantom, solflare, etc.)
    /// </summary>
    [MaxLength(50)]
    [Column("wallet_provider")]
    public string WalletProvider { get; set; } = "phantom";

    /// <summary>
    /// Signature from wallet (for verification)
    /// </summary>
    [MaxLength(128)]
    [Column("signature")]
    public string? Signature { get; set; }

    /// <summary>
    /// Optional redirect URL after successful connection
    /// </summary>
    [MaxLength(500)]
    [Column("redirect_url")]
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Optional metadata (JSON) for client-specific data
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

    /// <summary>
    /// IP address that initiated the request (for audit/security)
    /// </summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent that initiated the request
    /// </summary>
    [MaxLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When this intent expires (typically 10 minutes)
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the intent was completed (wallet linked)
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Wallet connect intent status constants
/// </summary>
public static class WalletConnectIntentStatus
{
    /// <summary>Initial state - waiting for user to connect wallet</summary>
    public const string Pending = "pending";

    /// <summary>User initiated connection, waiting for callback</summary>
    public const string AwaitingCallback = "awaiting_callback";

    /// <summary>Callback received, processing signature</summary>
    public const string Processing = "processing";

    /// <summary>Successfully completed - wallet linked</summary>
    public const string Completed = "completed";

    /// <summary>Intent expired before completion</summary>
    public const string Expired = "expired";

    /// <summary>User cancelled the connection</summary>
    public const string Cancelled = "cancelled";

    /// <summary>Failed - signature invalid or other error</summary>
    public const string Failed = "failed";
}

/// <summary>
/// Purpose of the wallet connection
/// </summary>
public static class WalletConnectPurpose
{
    /// <summary>Simple wallet connection/linking</summary>
    public const string Connect = "connect";

    /// <summary>Sign a message (e.g., for auth)</summary>
    public const string Sign = "sign";

    /// <summary>Sign a transaction</summary>
    public const string Transaction = "transaction";

    /// <summary>Support a bird (donate)</summary>
    public const string Support = "support";

    /// <summary>P2P payment</summary>
    public const string Payment = "payment";
}
