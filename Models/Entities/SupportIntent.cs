using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents an intent to support (donate to) a bird
/// Supports both wallet-based and custodial payments
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

    [Column("support_amount")]
    public decimal SupportAmount { get; set; }

    /// <summary>
    /// Platform fee (5% of support amount)
    /// </summary>
    [Column("platform_support_amount")]
    public decimal PlatformFee { get; set; }

    /// <summary>
    /// Platform fee percentage (e.g., 5 for 5%)
    /// </summary>
    [Column("platform_fee_percent")]
    public decimal PlatformFeePercent { get; set; } = 5m;

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

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = SupportPaymentMethod.Pending;

    [Column("solana_signature")]
    public string? SolanaSignature { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; }

    [Column("serialized_transaction")]
    public string? SerializedTransaction { get; set; }

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
