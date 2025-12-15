using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Payout
{
    public class PayoutBalance
    {
        [Key]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AvailableBalance { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal PendingBalance { get; set; } = 0;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        public DateTime? LastPayoutDate { get; set; }

        [Required]
        public DateTime NextPayoutDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties
        public bool MinimumReached => AvailableBalance >= 20.00m;

        public decimal TotalBalance => AvailableBalance + PendingBalance;
    }
}
