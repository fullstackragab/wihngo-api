using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents a single entry in the user's USDC ledger
/// Positive amounts are credits, negative amounts are debits
/// </summary>
[Table("ledger_entries")]
public class LedgerEntry
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Amount in USDC (positive = credit, negative = debit)
    /// </summary>
    [Required]
    [Column("amount_usdc")]
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Type of entry: Payment, PaymentReceived, Fee, Refund, Adjustment, Deposit, Withdrawal
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column("entry_type")]
    public string EntryType { get; set; } = string.Empty;

    /// <summary>
    /// Type of reference: P2PPayment, GasSponsorship, Manual
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column("reference_type")]
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the referenced entity (payment, sponsorship, etc.)
    /// </summary>
    [Required]
    [Column("reference_id")]
    public Guid ReferenceId { get; set; }

    /// <summary>
    /// Running balance after this entry (denormalized for query performance)
    /// </summary>
    [Required]
    [Column("balance_after")]
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Human-readable description of the entry
    /// </summary>
    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}

/// <summary>
/// Ledger entry type constants
/// </summary>
public static class LedgerEntryType
{
    public const string Payment = "Payment";
    public const string PaymentReceived = "PaymentReceived";
    public const string Fee = "Fee";
    public const string Refund = "Refund";
    public const string Adjustment = "Adjustment";
    public const string Deposit = "Deposit";
    public const string Withdrawal = "Withdrawal";
}

/// <summary>
/// Reference type constants
/// </summary>
public static class LedgerReferenceType
{
    public const string P2PPayment = "P2PPayment";
    public const string GasSponsorship = "GasSponsorship";
    public const string Manual = "Manual";
}
