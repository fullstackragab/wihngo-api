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

        // Premium highlight fields
        public bool IsHighlighted { get; set; } = false;
        public int? HighlightOrder { get; set; }

        // Like and comment tracking
        public int LikeCount { get; set; } = 0;
        public int CommentCount { get; set; } = 0;

        // Navigation properties
        public List<StoryLike> Likes { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }
}
