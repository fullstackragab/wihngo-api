using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models
{
    [Table("memorial_fund_redirections")]
    public class MemorialFundRedirection
    {
        [Key]
        [Column("redirection_id")]
        public Guid RedirectionId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("bird_id")]
        public Guid BirdId { get; set; }

        [ForeignKey(nameof(BirdId))]
        public Bird? Bird { get; set; }

        [Required]
        [Column("previous_owner_id")]
        public Guid PreviousOwnerId { get; set; }

        [ForeignKey(nameof(PreviousOwnerId))]
        public User? PreviousOwner { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column("remaining_balance")]
        public decimal RemainingBalance { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("redirection_type")]
        public string RedirectionType { get; set; } = string.Empty; // 'emergency_fund', 'owner_keeps', 'charity'

        [MaxLength(255)]
        [Column("charity_name")]
        public string? CharityName { get; set; }

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "pending"; // 'pending', 'processing', 'completed', 'failed'

        [MaxLength(255)]
        [Column("transaction_id")]
        public string? TransactionId { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
