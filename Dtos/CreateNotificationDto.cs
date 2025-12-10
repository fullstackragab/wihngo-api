namespace Wihngo.Dtos
{
    using System;
    using Wihngo.Models.Enums;

    public class CreateNotificationDto
    {
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
        public NotificationChannel Channels { get; set; } = NotificationChannel.InApp;
        public string? DeepLink { get; set; }
        public Guid? BirdId { get; set; }
        public Guid? StoryId { get; set; }
        public Guid? TransactionId { get; set; }
        public Guid? ActorUserId { get; set; }
        public string? Metadata { get; set; }
    }
}
