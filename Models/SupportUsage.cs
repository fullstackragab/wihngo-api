namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class SupportUsage
    {
        [Key]
        public Guid UsageId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        [Required]
        public Guid ReportedBy { get; set; }

        [ForeignKey(nameof(ReportedBy))]
        public User? Reporter { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
