using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Tracks gas sponsorship events where the platform pays SOL fees for users
/// </summary>
[Table("gas_sponsorships")]
public class GasSponsorship
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Amount of SOL sponsored for transaction fee
    /// </summary>
    [Required]
    [Column("sponsored_sol_amount")]
    public decimal SponsoredSolAmount { get; set; }

    /// <summary>
    /// USDC fee charged to user for sponsorship (flat $0.01)
    /// </summary>
    [Column("fee_usdc_charged")]
    public decimal FeeUsdcCharged { get; set; } = 0.01m;

    /// <summary>
    /// Platform wallet public key used for sponsorship
    /// </summary>
    [Required]
    [MaxLength(44)]
    [Column("sponsor_wallet_pubkey")]
    public string SponsorWalletPubkey { get; set; } = string.Empty;

    /// <summary>
    /// Whether an Associated Token Account was created for the recipient
    /// </summary>
    [Column("ata_created")]
    public bool AtaCreated { get; set; } = false;

    /// <summary>
    /// Address of the created ATA (if applicable)
    /// </summary>
    [MaxLength(44)]
    [Column("ata_address")]
    public string? AtaAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public P2PPayment? Payment { get; set; }
}
