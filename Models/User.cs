namespace Wihngo.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json;

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

        // Weekly support cap (how much this user can RECEIVE per week in baseline support)
        /// <summary>
        /// Maximum USDC this caretaker can receive per week in baseline support.
        /// Gifts are unlimited and do not count toward this cap.
        /// Default: 5 USDC
        /// </summary>
        public decimal WeeklyCap { get; set; } = 5.00m;

        // Smart feed preferences
        /// <summary>
        /// JSON array of preferred content language codes (ISO 639-1)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? PreferredLanguagesJson { get; set; }

        /// <summary>
        /// Helper property to access preferred languages as a list
        /// </summary>
        [NotMapped]
        public List<string> PreferredLanguages
        {
            get => string.IsNullOrEmpty(PreferredLanguagesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(PreferredLanguagesJson) ?? new List<string>();
            set => PreferredLanguagesJson = JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// User's country (ISO 3166-1 alpha-2 code)
        /// </summary>
        [MaxLength(10)]
        public string? Country { get; set; }

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
