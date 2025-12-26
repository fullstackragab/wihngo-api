using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("refund_requests")]
public class RefundRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("payment_id")]
    public Guid? PaymentId { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,6)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [Column("state")]
    [MaxLength(50)]
    public string State { get; set; } = "REQUESTED"; // REQUESTED, PROCESSING, COMPLETED, FAILED

    [Column("refund_method")]
    [MaxLength(50)]
    public string? RefundMethod { get; set; } // paypal, crypto

    [Column("refund_tx_hash")]
    [MaxLength(255)]
    public string? RefundTxHash { get; set; }

    [Column("provider_refund_id")]
    [MaxLength(255)]
    public string? ProviderRefundId { get; set; }

    [Column("refund_receipt_url")]
    [MaxLength(1000)]
    public string? RefundReceiptUrl { get; set; }

    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = false;

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
