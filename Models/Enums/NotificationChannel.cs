namespace Wihngo.Models.Enums
{
    [Flags]
    public enum NotificationChannel
    {
        None = 0,
        InApp = 1,
        Push = 2,
        Email = 4,
        Sms = 8
    }
}
