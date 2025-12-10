namespace Wihngo.Dtos
{
    using System;

    public class NotificationSettingsDto
    {
        public Guid SettingsId { get; set; }
        public TimeSpan QuietHoursStart { get; set; }
        public TimeSpan QuietHoursEnd { get; set; }
        public bool QuietHoursEnabled { get; set; }
        public int MaxPushPerDay { get; set; }
        public int MaxEmailPerDay { get; set; }
        public bool EnableNotificationGrouping { get; set; }
        public int GroupingWindowMinutes { get; set; }
        public bool EnableDailyDigest { get; set; }
        public TimeSpan DailyDigestTime { get; set; }
        public string? TimeZone { get; set; }
    }
}
