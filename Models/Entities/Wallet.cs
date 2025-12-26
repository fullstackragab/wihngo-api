using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents a user's linked crypto wallet (e.g., Phantom)
/// </summary>
[Table("wallets")]
public class Wallet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(44)]
    [Column("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("wallet_provider")]
    public string WalletProvider { get; set; } = "phantom";

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
