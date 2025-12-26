using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Represents a kind word (caring comment) left on a bird's story.
/// Part of the constrained, care-focused comment system.
/// </summary>
[Table("kind_words")]
public class KindWord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("bird_id")]
    public Guid BirdId { get; set; }

    [Required]
    [Column("author_user_id")]
    public Guid AuthorUserId { get; set; }

    /// <summary>
    /// The kind word text. Max 200 characters.
    /// Must not contain URLs or be only whitespace.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag. Deleted kind words are not shown.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Visibility flag. Can be toggled by owner or admin.
    /// </summary>
    [Column("is_visible")]
    public bool IsVisible { get; set; } = true;

    // Navigation properties
    public Bird? Bird { get; set; }
    public User? Author { get; set; }
}

/// <summary>
/// Represents a user blocked from posting kind words on a specific bird.
/// </summary>
[Table("kind_words_blocked_users")]
public class KindWordBlockedUser
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("bird_id")]
    public Guid BirdId { get; set; }

    [Required]
    [Column("blocked_user_id")]
    public Guid BlockedUserId { get; set; }

    [Column("blocked_at")]
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("blocked_by_user_id")]
    public Guid BlockedByUserId { get; set; }

    // Navigation properties
    public Bird? Bird { get; set; }
    public User? BlockedUser { get; set; }
    public User? BlockedByUser { get; set; }
}
