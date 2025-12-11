namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class CharityImpactStats
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalContributed { get; set; } = 0;

        [Required]
        public int BirdsHelped { get; set; } = 0;

        [Required]
        public int SheltersSupported { get; set; } = 0;

        [Required]
        public int ConservationProjects { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
