namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Tracks which birds a user follows for personalized feed content.
    /// Different from "Love" - following means wanting to see stories about this bird.
    /// </summary>
    public class UserBirdFollow
    {
        [Key]
        public Guid FollowId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
