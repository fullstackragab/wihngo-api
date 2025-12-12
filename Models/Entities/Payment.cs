using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Required]
    [Column("payment_method")]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // paypal, solana, base

    [Column("payer_identifier")]
    [MaxLength(500)]
    public string? PayerIdentifier { get; set; } // email or wallet

    [Column("tx_hash")]
    [MaxLength(255)]
    public string? TxHash { get; set; }

    [Column("provider_tx_id")]
    [MaxLength(255)]
    public string? ProviderTxId { get; set; } // PayPal transaction ID

    [Column("token")]
    [MaxLength(20)]
    public string? Token { get; set; } // USDC, EURC, etc.

    [Column("chain")]
    [MaxLength(50)]
    public string? Chain { get; set; } // solana, base, ethereum

    [Column("amount_crypto", TypeName = "decimal(20,10)")]
    public decimal? AmountCrypto { get; set; }

    [Column("fiat_value_at_payment", TypeName = "decimal(18,6)")]
    public decimal? FiatValueAtPayment { get; set; }

    [Column("block_slot")]
    public long? BlockSlot { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Invoice? Invoice { get; set; }
}
