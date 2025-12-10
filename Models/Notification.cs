namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Wihngo.Models.Enums;

    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;

        [Required]
        public NotificationChannel Channels { get; set; } = NotificationChannel.InApp;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [MaxLength(500)]
        public string? DeepLink { get; set; }

        // Related entity references for grouping and linking
        public Guid? BirdId { get; set; }
        public Guid? StoryId { get; set; }
        public Guid? TransactionId { get; set; }
        public Guid? ActorUserId { get; set; } // The user who triggered the notification

        // Grouping support
        public Guid? GroupId { get; set; } // For batching similar notifications
        public int GroupCount { get; set; } = 1; // Number of items in this group

        // Delivery tracking
        public bool PushSent { get; set; } = false;
        public bool EmailSent { get; set; } = false;
        public bool SmsSent { get; set; } = false;
        public DateTime? PushSentAt { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public DateTime? SmsSentAt { get; set; }

        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional data

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
