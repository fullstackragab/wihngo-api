namespace Wihngo.Dtos
{
    using Wihngo.Models.Enums;

    public class UpdateNotificationPreferenceDto
    {
        public NotificationType NotificationType { get; set; }
        public bool? InAppEnabled { get; set; }
        public bool? PushEnabled { get; set; }
        public bool? EmailEnabled { get; set; }
        public bool? SmsEnabled { get; set; }
    }
}
