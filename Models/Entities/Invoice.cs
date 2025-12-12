using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wihngo.Models.Enums;

namespace Wihngo.Models.Entities;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("invoice_number")]
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("bird_id")]
    public Guid? BirdId { get; set; }

    [Required]
    [Column("amount_fiat", TypeName = "decimal(18,6)")]
    public decimal AmountFiat { get; set; }

    [Required]
    [Column("fiat_currency")]
    [MaxLength(10)]
    public string FiatCurrency { get; set; } = "USD";

    [Column("amount_fiat_at_settlement", TypeName = "decimal(18,6)")]
    public decimal? AmountFiatAtSettlement { get; set; }

    [Column("settlement_currency")]
    [MaxLength(10)]
    public string? SettlementCurrency { get; set; }

    [Required]
    [Column("state")]
    [MaxLength(50)]
    public string State { get; set; } = nameof(InvoicePaymentState.CREATED);

    [Column("preferred_payment_methods", TypeName = "jsonb")]
    public string? PreferredPaymentMethods { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("issued_pdf_url")]
    [MaxLength(1000)]
    public string? IssuedPdfUrl { get; set; }

    [Column("issued_at")]
    public DateTime? IssuedAt { get; set; }

    [Column("receipt_notes")]
    public string? ReceiptNotes { get; set; }

    [Column("is_tax_deductible")]
    public bool IsTaxDeductible { get; set; } = false;

    [Column("solana_reference")]
    [MaxLength(255)]
    public string? SolanaReference { get; set; }

    [Column("base_payment_data", TypeName = "jsonb")]
    public string? BasePaymentData { get; set; }

    [Column("paypal_order_id")]
    [MaxLength(255)]
    public string? PayPalOrderId { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
