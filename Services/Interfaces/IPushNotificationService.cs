namespace Wihngo.Services.Interfaces
{
    using System.Threading.Tasks;
    using Wihngo.Models;

    public interface IPushNotificationService
    {
        /// <summary>
        /// Send push notification to all active devices for a user
        /// </summary>
        Task SendPushNotificationAsync(Notification notification);

        /// <summary>
        /// Send push notification to specific device token
        /// </summary>
        Task SendToDeviceAsync(string pushToken, string title, string message, object? data = null);

        /// <summary>
        /// Validate if a push token is valid (Expo format)
        /// </summary>
        bool IsValidPushToken(string pushToken);

        /// <summary>
        /// Send invoice issued notification to user
        /// </summary>
        Task SendInvoiceIssuedNotificationAsync(Guid userId, string invoiceNumber);
    }
}
