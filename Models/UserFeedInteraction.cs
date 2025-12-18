namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Tracks user interactions with stories for feed personalization.
    /// Used to learn user preferences and improve feed ranking.
    /// </summary>
    public class UserFeedInteraction
    {
        [Key]
        public Guid InteractionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        [ForeignKey(nameof(StoryId))]
        public Story? Story { get; set; }

        /// <summary>
        /// Type of interaction: "view", "like", "comment", "skip", "share"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string InteractionType { get; set; } = string.Empty;

        /// <summary>
        /// Time spent viewing the story in seconds (for implicit feedback)
        /// </summary>
        public int? ViewDurationSeconds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
