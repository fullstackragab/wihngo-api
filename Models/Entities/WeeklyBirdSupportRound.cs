using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Tracks how many times a bird has received support in a given week.
/// Used to implement the 2-round weekly support system.
/// </summary>
[Table("weekly_bird_support_rounds")]
public class WeeklyBirdSupportRound
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("bird_id")]
    public Guid BirdId { get; set; }

    [Required]
    [Column("week_start_date")]
    public DateTime WeekStartDate { get; set; }

    /// <summary>
    /// How many times this bird has been supported this week (0-2).
    /// </summary>
    [Column("times_supported")]
    public int TimesSupported { get; set; } = 0;

    [Column("last_supported_at")]
    public DateTime? LastSupportedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
