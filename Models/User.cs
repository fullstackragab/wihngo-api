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

        // Security fields
        public bool EmailConfirmed { get; set; } = false;
        
        [MaxLength(500)]
        public string? EmailConfirmationToken { get; set; }
        
        public DateTime? EmailConfirmationTokenExpiry { get; set; }
        
        public bool IsAccountLocked { get; set; } = false;
        
        public int FailedLoginAttempts { get; set; } = 0;
        
        public DateTime? LockoutEnd { get; set; }
        
        public DateTime? LastLoginAt { get; set; }
        
        [MaxLength(500)]
        public string? RefreshTokenHash { get; set; }
        
        public DateTime? RefreshTokenExpiry { get; set; }
        
        [MaxLength(500)]
        public string? PasswordResetToken { get; set; }
        
        public DateTime? PasswordResetTokenExpiry { get; set; }
        
        public DateTime? LastPasswordChangeAt { get; set; }

        // Navigation properties
        public List<Bird> Birds { get; set; } = new();

        public List<Story> Stories { get; set; } = new();

        // Transactions where this user is the supporter
        public List<SupportTransaction> SupportTransactions { get; set; } = new();

        public List<Love> Loves { get; set; } = new();

        public List<SupportUsage> ReportedSupportUsage { get; set; } = new();

        public List<StoryLike> StoryLikes { get; set; } = new();

        public List<Comment> Comments { get; set; } = new();

        public List<CommentLike> CommentLikes { get; set; } = new();

        public string GetDisplayName() => Name;
    }
}
