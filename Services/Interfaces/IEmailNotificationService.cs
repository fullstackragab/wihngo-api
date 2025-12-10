namespace Wihngo.Services.Interfaces
{
    using System.Threading.Tasks;
    using Wihngo.Models;

    public interface IEmailNotificationService
    {
        /// <summary>
        /// Send email notification to user
        /// </summary>
        Task SendEmailNotificationAsync(Notification notification);

        /// <summary>
        /// Send email to specific address
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);

        /// <summary>
        /// Send daily digest email
        /// </summary>
        Task SendDailyDigestAsync(System.Guid userId);
    }
}
