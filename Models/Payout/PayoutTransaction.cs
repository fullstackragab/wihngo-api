using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wihngo.Models.Enums;

namespace Wihngo.Models.Payout
{
    public class PayoutTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public Guid PayoutMethodId { get; set; }

        [ForeignKey(nameof(PayoutMethodId))]
        public PayoutMethod? PayoutMethod { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        [Required]
        public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

        // Fee breakdown
        [Range(0, double.MaxValue)]
        public decimal PlatformFee { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal ProviderFee { get; set; } = 0;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal NetAmount { get; set; }

        // Processing details
        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper methods
        public bool IsTerminal => Status == PayoutStatus.Completed 
                                 || Status == PayoutStatus.Failed 
                                 || Status == PayoutStatus.Cancelled;

        public bool IsProcessable => Status == PayoutStatus.Pending 
                                    || Status == PayoutStatus.Failed;
    }
}
