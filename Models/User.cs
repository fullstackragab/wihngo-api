namespace Wihngo.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ProfileImage { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<Bird> Birds { get; set; } = new();

        public List<Story> Stories { get; set; } = new();

        // Transactions where this user is the supporter
        public List<SupportTransaction> SupportTransactions { get; set; } = new();

        public string GetDisplayName() => Name;
    }
}
