using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents a P2P USDC payment between two users on Solana
/// </summary>
[Table("p2p_payments")]
public class P2PPayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    // Parties
    [Required]
    [Column("sender_user_id")]
    public Guid SenderUserId { get; set; }

    [Required]
    [Column("recipient_user_id")]
    public Guid RecipientUserId { get; set; }

    [MaxLength(44)]
    [Column("sender_wallet_pubkey")]
    public string? SenderWalletPubkey { get; set; }

    [MaxLength(44)]
    [Column("recipient_wallet_pubkey")]
    public string? RecipientWalletPubkey { get; set; }

    // Amounts (USDC has 6 decimals)
    [Required]
    [Column("amount_usdc")]
    public decimal AmountUsdc { get; set; }

    [Column("fee_usdc")]
    public decimal FeeUsdc { get; set; } = 0;

    /// <summary>
    /// Total amount = AmountUsdc + FeeUsdc
    /// </summary>
    [NotMapped]
    public decimal TotalUsdc => AmountUsdc + FeeUsdc;

    // Status: pending, awaiting_signature, submitted, confirming, confirmed, completed, failed, expired, cancelled
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = PaymentStatus.Pending;

    // Blockchain details
    [MaxLength(88)]
    [Column("solana_signature")]
    public string? SolanaSignature { get; set; }

    [Column("block_slot")]
    public long? BlockSlot { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    // Gas sponsorship
    [Column("gas_sponsored")]
    public bool GasSponsored { get; set; } = false;

    // Serialized unsigned transaction (base64)
    [Column("serialized_transaction")]
    public string? SerializedTransaction { get; set; }

    // Metadata
    [MaxLength(255)]
    [Column("memo")]
    public string? Memo { get; set; }

    // Timestamps
    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Sender { get; set; }
    public User? Recipient { get; set; }
    public GasSponsorship? GasSponsorshipRecord { get; set; }

    /// <summary>
    /// Check if payment has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;
}

/// <summary>
/// Payment status constants
/// </summary>
public static class PaymentStatus
{
    public const string Pending = "pending";
    public const string AwaitingSignature = "awaiting_signature";
    public const string Submitted = "submitted";
    public const string Confirming = "confirming";
    public const string Confirmed = "confirmed";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
}
