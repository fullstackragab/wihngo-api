namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class PremiumStyle
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        [MaxLength(50)]
        public string? FrameId { get; set; }

        [MaxLength(50)]
        public string? BadgeId { get; set; }

        [MaxLength(7)]
        public string? HighlightColor { get; set; }

        [MaxLength(50)]
        public string? ThemeId { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
