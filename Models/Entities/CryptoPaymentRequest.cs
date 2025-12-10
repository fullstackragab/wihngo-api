using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("crypto_payment_requests")]
public class CryptoPaymentRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("bird_id")]
    public Guid? BirdId { get; set; }

    [Required]
    [Column("amount_usd", TypeName = "decimal(10,2)")]
    public decimal AmountUsd { get; set; }

    [Required]
    [Column("amount_crypto", TypeName = "decimal(20,10)")]
    public decimal AmountCrypto { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Required]
    [Column("exchange_rate", TypeName = "decimal(20,2)")]
    public decimal ExchangeRate { get; set; }

    [Required]
    [Column("wallet_address")]
    [MaxLength(255)]
    public string WalletAddress { get; set; } = string.Empty;

    [Column("user_wallet_address")]
    [MaxLength(255)]
    public string? UserWalletAddress { get; set; }

    [Required]
    [Column("qr_code_data")]
    public string QrCodeData { get; set; } = string.Empty;

    [Required]
    [Column("payment_uri")]
    public string PaymentUri { get; set; } = string.Empty;

    [Column("transaction_hash")]
    [MaxLength(255)]
    public string? TransactionHash { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    [Required]
    [Column("required_confirmations")]
    public int RequiredConfirmations { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Required]
    [Column("purpose")]
    [MaxLength(50)]
    public string Purpose { get; set; } = string.Empty;

    [Column("plan")]
    [MaxLength(20)]
    public string? Plan { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
