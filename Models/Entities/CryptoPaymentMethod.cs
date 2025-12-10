using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("crypto_payment_methods")]
public class CryptoPaymentMethod
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("wallet_address")]
    [MaxLength(255)]
    public string WalletAddress { get; set; } = string.Empty;

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Column("label")]
    [MaxLength(100)]
    public string? Label { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("verified")]
    public bool Verified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
