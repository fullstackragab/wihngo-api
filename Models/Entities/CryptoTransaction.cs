using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("crypto_transactions")]
public class CryptoTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("payment_request_id")]
    public Guid PaymentRequestId { get; set; }

    [Required]
    [Column("transaction_hash")]
    [MaxLength(255)]
    public string TransactionHash { get; set; } = string.Empty;

    [Required]
    [Column("from_address")]
    [MaxLength(255)]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    [Column("to_address")]
    [MaxLength(255)]
    public string ToAddress { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(20,10)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    [Column("block_number")]
    public long? BlockNumber { get; set; }

    [Column("block_hash")]
    [MaxLength(255)]
    public string? BlockHash { get; set; }

    [Column("fee", TypeName = "decimal(20,10)")]
    public decimal? Fee { get; set; }

    [Column("gas_used")]
    public long? GasUsed { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("raw_transaction", TypeName = "jsonb")]
    public string? RawTransaction { get; set; }

    [Column("detected_at")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [ForeignKey("PaymentRequestId")]
    public CryptoPaymentRequest? PaymentRequest { get; set; }
}
