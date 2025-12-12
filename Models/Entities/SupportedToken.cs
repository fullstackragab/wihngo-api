using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("supported_tokens")]
public class SupportedToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("token_symbol")]
    [MaxLength(20)]
    public string TokenSymbol { get; set; } = string.Empty; // USDC, EURC

    [Required]
    [Column("chain")]
    [MaxLength(50)]
    public string Chain { get; set; } = string.Empty; // solana, base

    [Required]
    [Column("mint_address")]
    [MaxLength(255)]
    public string MintAddress { get; set; } = string.Empty; // SPL mint or ERC-20 contract

    [Column("merchant_receiving_address")]
    [MaxLength(255)]
    public string? MerchantReceivingAddress { get; set; }

    [Column("decimals")]
    public int Decimals { get; set; } = 6;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("tolerance_percent", TypeName = "decimal(5,2)")]
    public decimal TolerancePercent { get; set; } = 0.5m; // 0.5% tolerance

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
