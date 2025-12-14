namespace Wihngo.Dtos
{
    using System;
    using Wihngo.Models.Enums;

    public class NotificationDto
    {
        public Guid NotificationId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? DeepLink { get; set; }
        public Guid? BirdId { get; set; }
        public Guid? StoryId { get; set; }
        public Guid? ActorUserId { get; set; }
        public string? ActorUserName { get; set; }
        public int GroupCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MarkAsReadDto
    {
        public Guid NotificationId { get; set; }
    }
}
