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

        [MaxLength(200)]
        [Column("location")]
        public string? Location { get; set; }

        [MaxLength(100)]
        [Column("age")]
        public string? Age { get; set; }

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

        // Maximum media uploads per bird (all birds get same limit now - "All birds are equal")
        public int MaxMediaCount { get; set; } = 5;

        // Track support amounts in cents to avoid floating point issues
        public long DonationCents { get; set; } = 0;

        // Whether this bird is currently accepting support (controlled by owner)
        [Column("support_enabled")]
        public bool SupportEnabled { get; set; } = true;

        // Optional QR code URL for offline-to-online profile link
        [MaxLength(1000)]
        public string? QrCodeUrl { get; set; }

        // Activity tracking
        [Column("last_activity_at")]
        public DateTime? LastActivityAt { get; set; }

        // Memorial fields
        [Column("is_memorial")]
        public bool IsMemorial { get; set; } = false;

        [Column("memorial_date")]
        public DateTime? MemorialDate { get; set; }

        [MaxLength(500)]
        [Column("memorial_reason")]
        public string? MemorialReason { get; set; }

        [MaxLength(50)]
        [Column("funds_redirection_choice")]
        public string? FundsRedirectionChoice { get; set; }

        // Navigation properties for memorial
        public List<MemorialMessage> MemorialMessages { get; set; } = new();
        public List<MemorialFundRedirection> FundRedirections { get; set; } = new();
    }
}
