namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class BirdPremiumSubscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public User? Owner { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active | canceled | past_due | expired

        [Required]
        [MaxLength(50)]
        public string Plan { get; set; } = "monthly";

        [MaxLength(50)]
        public string? Provider { get; set; }

        [MaxLength(200)]
        public string? ProviderSubscriptionId { get; set; }

        // Price in cents
        public long PriceCents { get; set; } = 300; // default $3 monthly

        // Duration in days
        public int DurationDays { get; set; } = 30;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime CurrentPeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(1);
        public DateTime? CanceledAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
