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

        [Required]
        [MaxLength(1000)]
        public string ImageUrl { get; set; } = string.Empty;

        public int LovedCount { get; set; } = 0;

        public int SupportedCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Story> Stories { get; set; } = new();

        public List<SupportTransaction> SupportTransactions { get; set; } = new();

        public List<Love> Loves { get; set; } = new();

        public List<SupportUsage> SupportUsages { get; set; } = new();

        // Premium fields
        public bool IsPremium { get; set; } = false;

        // Stored as a simple JSON blob via EF core owned type or string
        // For simplicity keep as nullable JSON string to avoid additional migration complexity
        public string? PremiumStyleJson { get; set; }

        // When premium expires (nullable for lifetime)
        public DateTime? PremiumExpiresAt { get; set; }

        // Plan name such as "monthly", "yearly", "lifetime"
        [MaxLength(50)]
        public string? PremiumPlan { get; set; }

        // Allow premium users more media uploads
        public int MaxMediaCount { get; set; } = 5;

        // Track donations in cents to avoid floating point issues
        public long DonationCents { get; set; } = 0;

        // Optional QR code URL for offline-to-online profile link
        [MaxLength(1000)]
        public string? QrCodeUrl { get; set; }
    }
}
