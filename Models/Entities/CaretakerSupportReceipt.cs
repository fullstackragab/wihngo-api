using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Wihngo.Models.Entities;

/// <summary>
/// Tracks support received by a caretaker (bird owner).
/// Used to enforce weekly baseline caps and distinguish between baseline support and gifts.
///
/// Key invariants:
/// - Birds never multiply money
/// - One user = one wallet = capped baseline support
/// - Baseline support counts toward weekly cap
/// - Gifts are unlimited and do NOT count toward weekly cap
/// </summary>
[Table("caretaker_support_receipts")]
public class CaretakerSupportReceipt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The caretaker (bird owner) who RECEIVES the support
    /// </summary>
    [Column("caretaker_user_id")]
    public Guid CaretakerUserId { get; set; }

    /// <summary>
    /// The user who SENDS the support
    /// </summary>
    [Column("supporter_user_id")]
    public Guid SupporterUserId { get; set; }

    /// <summary>
    /// Optional reference to the bird being supported (for tracking only, does not affect payout)
    /// </summary>
    [Column("bird_id")]
    public Guid? BirdId { get; set; }

    /// <summary>
    /// Solana transaction signature for on-chain verification
    /// </summary>
    [Column("tx_signature")]
    [MaxLength(128)]
    public string TxSignature { get; set; } = string.Empty;

    /// <summary>
    /// Amount in USDC
    /// </summary>
    [Column("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction type: "baseline" or "gift"
    /// - baseline: Counts toward weekly cap
    /// - gift: Does NOT count toward weekly cap
    /// </summary>
    [Column("transaction_type")]
    [MaxLength(20)]
    public string TransactionType { get; set; } = CaretakerSupportTransactionType.Baseline;

    /// <summary>
    /// ISO week identifier (YYYY-WW format, e.g., "2025-02")
    /// Used for weekly cap calculations
    /// </summary>
    [Column("week_id")]
    [MaxLength(10)]
    public string WeekId { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to the SupportIntent that generated this receipt
    /// </summary>
    [Column("support_intent_id")]
    public Guid? SupportIntentId { get; set; }

    /// <summary>
    /// Whether this transaction has been verified on-chain
    /// </summary>
    [Column("verified_on_chain")]
    public bool VerifiedOnChain { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (optional, for EF if needed)
    [ForeignKey(nameof(CaretakerUserId))]
    public User? Caretaker { get; set; }

    [ForeignKey(nameof(SupporterUserId))]
    public User? Supporter { get; set; }

    [ForeignKey(nameof(BirdId))]
    public Bird? Bird { get; set; }

    /// <summary>
    /// Helper method to compute the current ISO week ID
    /// </summary>
    public static string GetCurrentWeekId()
    {
        return GetWeekId(DateTime.UtcNow);
    }

    /// <summary>
    /// Compute ISO week ID for a given date (YYYY-WW format)
    /// </summary>
    public static string GetWeekId(DateTime date)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = date.Year;

        // Handle edge case: week 1 might be in the previous year
        if (week == 1 && date.Month == 12)
        {
            year++;
        }
        // Handle edge case: last week might be in the next year
        else if (week >= 52 && date.Month == 1)
        {
            year--;
        }

        return $"{year}-{week:D2}";
    }
}

/// <summary>
/// Transaction type constants
/// </summary>
public static class CaretakerSupportTransactionType
{
    /// <summary>
    /// Baseline support - counts toward weekly cap
    /// </summary>
    public const string Baseline = "baseline";

    /// <summary>
    /// Gift - does NOT count toward weekly cap
    /// </summary>
    public const string Gift = "gift";
}
