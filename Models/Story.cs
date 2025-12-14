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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Premium highlight fields
        public bool IsHighlighted { get; set; } = false;
        public int? HighlightOrder { get; set; }

        // Navigation property for many-to-many relationship with Birds
        public ICollection<StoryBird> StoryBirds { get; set; } = new List<StoryBird>();
    }
}
