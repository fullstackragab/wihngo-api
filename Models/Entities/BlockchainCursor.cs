using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("blockchain_cursors")]
public class BlockchainCursor
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("chain")]
    [MaxLength(50)]
    public string Chain { get; set; } = string.Empty; // solana, base

    [Required]
    [Column("cursor_type")]
    [MaxLength(50)]
    public string CursorType { get; set; } = string.Empty; // block, slot

    [Column("last_processed_value")]
    public long LastProcessedValue { get; set; } = 0;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
