namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class StoryLike
    {
        [Key]
        public Guid LikeId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StoryId { get; set; }

        [ForeignKey(nameof(StoryId))]
        public Story? Story { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
