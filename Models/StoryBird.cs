namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Junction table for many-to-many relationship between Stories and Birds
    /// </summary>
    public class StoryBird
    {
        [Key]
        public Guid StoryBirdId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StoryId { get; set; }

        [ForeignKey(nameof(StoryId))]
        public Story? Story { get; set; }

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
