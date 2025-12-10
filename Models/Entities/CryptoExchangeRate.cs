using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("crypto_exchange_rates")]
public class CryptoExchangeRate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("usd_rate", TypeName = "decimal(20,2)")]
    public decimal UsdRate { get; set; }

    [Required]
    [Column("source")]
    [MaxLength(50)]
    public string Source { get; set; } = "coingecko";

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
