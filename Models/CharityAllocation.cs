namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class CharityAllocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SubscriptionId { get; set; }

        [ForeignKey(nameof(SubscriptionId))]
        public BirdPremiumSubscription? Subscription { get; set; }

        [Required]
        [MaxLength(255)]
        public string CharityName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
    }
}
