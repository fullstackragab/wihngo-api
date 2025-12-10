using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("platform_wallets")]
public class PlatformWallet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Required]
    [Column("address")]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Column("private_key_encrypted")]
    public string? PrivateKeyEncrypted { get; set; }

    [Column("derivation_path")]
    [MaxLength(100)]
    public string? DerivationPath { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
