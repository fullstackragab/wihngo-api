namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class NotificationSettings
    {
        [Key]
        public Guid SettingsId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Quiet hours (local time)
        public TimeSpan QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0); // 10 PM
        public TimeSpan QuietHoursEnd { get; set; } = new TimeSpan(8, 0, 0); // 8 AM
        public bool QuietHoursEnabled { get; set; } = true;

        // Daily limits
        public int MaxPushPerDay { get; set; } = 5;
        public int MaxEmailPerDay { get; set; } = 2;

        // Grouping preferences
        public bool EnableNotificationGrouping { get; set; } = true;
        public int GroupingWindowMinutes { get; set; } = 60;

        // Email digest
        public bool EnableDailyDigest { get; set; } = false;
        public TimeSpan DailyDigestTime { get; set; } = new TimeSpan(9, 0, 0); // 9 AM

        [MaxLength(100)]
        public string? TimeZone { get; set; } = "UTC";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
