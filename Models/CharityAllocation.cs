namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Tracks charity donations from the platform (e.g., portion of support transactions)
    /// No longer tied to premium subscriptions - "All birds are equal"
    /// </summary>
    public class CharityAllocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Optional reference to a support transaction or other source
        /// </summary>
        public Guid? SourceId { get; set; }

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
