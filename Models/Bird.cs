namespace Wihngo.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Bird
    {
        [Key]
        public Guid BirdId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public User? Owner { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Species { get; set; }

        [MaxLength(500)]
        public string? Tagline { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        public int LovedCount { get; set; } = 0;

        public int SupportedCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Story> Stories { get; set; } = new();

        public List<SupportTransaction> SupportTransactions { get; set; } = new();

        public List<Love> Loves { get; set; } = new();

        public List<SupportUsage> SupportUsages { get; set; } = new();
    }
}
