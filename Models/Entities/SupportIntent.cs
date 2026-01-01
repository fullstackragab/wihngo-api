using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents an intent to support (donate to) a bird.
///
/// Bird-First Payment Model:
/// - 100% of bird_amount goes to bird owner (never deducted)
/// - wihngo_support_amount is optional and additive (minimum $0.05 if > 0)
/// - Total = bird_amount + wihngo_support_amount
/// - Two separate on-chain transfers
/// </summary>
[Table("support_intents")]
public class SupportIntent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("supporter_user_id")]
    public Guid SupporterUserId { get; set; }

    [Column("bird_id")]
    public Guid BirdId { get; set; }

    [Column("recipient_user_id")]
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Amount going to bird owner (100%, never deducted from)
    /// </summary>
    [Column("bird_amount")]
    public decimal BirdAmount { get; set; }

    /// <summary>
    /// Optional support for Wihngo platform (additive, not deducted from bird amount).
    /// Minimum $0.05 if provided, can be $0.
    /// </summary>
    [Column("wihngo_support_amount")]
    public decimal WihngoSupportAmount { get; set; }

    /// <summary>
    /// Total amount paid = bird_amount + wihngo_support_amount
    /// </summary>
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USDC";

    [Column("status")]
    public string Status { get; set; } = SupportIntentStatus.Pending;

    [Column("sender_wallet_pubkey")]
    public string? SenderWalletPubkey { get; set; }

    [Column("recipient_wallet_pubkey")]
    public string? RecipientWalletPubkey { get; set; }

    /// <summary>
    /// Wihngo treasury wallet for receiving optional platform support
    /// </summary>
    [Column("wihngo_wallet_pubkey")]
    public string? WihngoWalletPubkey { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = SupportPaymentMethod.Pending;

    /// <summary>
    /// Solana signature for bird transfer
    /// </summary>
    [Column("solana_signature")]
    public string? SolanaSignature { get; set; }

    /// <summary>
    /// Solana signature for Wihngo support transfer (if applicable)
    /// </summary>
    [Column("wihngo_solana_signature")]
    public string? WihngoSolanaSignature { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; }

    [Column("serialized_transaction")]
    public string? SerializedTransaction { get; set; }

    /// <summary>
    /// Idempotency key for preventing duplicate intents and submissions
    /// </summary>
    [Column("idempotency_key")]
    [MaxLength(64)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Error message for failed intents
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Support intent status constants
/// </summary>
public static class SupportIntentStatus
{
    public const string Pending = "pending";
    public const string AwaitingPayment = "awaiting_payment";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
}

/// <summary>
/// Payment method constants
/// </summary>
public static class SupportPaymentMethod
{
    public const string Pending = "pending";
    public const string Wallet = "wallet";
    public const string Custodial = "custodial";
}
