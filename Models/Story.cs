namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Wihngo.Models.Enums;

    public class Story
    {
        [Key]
        public Guid StoryId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AuthorId { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public User? Author { get; set; }

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional mood/category for the story. Null means no mood selected.
        /// </summary>
        public StoryMode? Mode { get; set; }

        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? VideoUrl { get; set; }

        [MaxLength(1000)]
        public string? AudioUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Like and comment tracking
        public int LikeCount { get; set; } = 0;
        public int CommentCount { get; set; } = 0;

        // Smart feed fields
        /// <summary>
        /// ISO 639-1 language code detected from content (e.g., "en", "ar", "es")
        /// </summary>
        [MaxLength(10)]
        public string? Language { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 country code from author's profile (e.g., "US", "SA", "MX")
        /// </summary>
        [MaxLength(10)]
        public string? Country { get; set; }

        // ===========================================
        // MODERATION FIELDS
        // ===========================================

        /// <summary>
        /// Moderation status: pending (default), approved, rejected, changes_requested
        /// Only approved stories are visible publicly
        /// </summary>
        public StoryStatus Status { get; set; } = StoryStatus.Pending;

        /// <summary>
        /// Admin note explaining rejection or requested changes
        /// </summary>
        [MaxLength(1000)]
        public string? AdminReviewNote { get; set; }

        /// <summary>
        /// Admin who reviewed this story
        /// </summary>
        public Guid? ReviewedByAdminId { get; set; }

        [ForeignKey(nameof(ReviewedByAdminId))]
        public User? ReviewedByAdmin { get; set; }

        /// <summary>
        /// When the story was reviewed by admin
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        // Navigation properties
        public List<StoryLike> Likes { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }
}
