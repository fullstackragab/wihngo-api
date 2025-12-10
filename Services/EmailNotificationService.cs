namespace Wihngo.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    /// <summary>
    /// Email notification service - placeholder implementation
    /// In production, integrate with SendGrid, AWS SES, or similar service
    /// </summary>
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            AppDbContext db,
            IConfiguration configuration,
            ILogger<EmailNotificationService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailNotificationAsync(Notification notification)
        {
            try
            {
                var user = await _db.Users.FindAsync(notification.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User {notification.UserId} not found for email notification");
                    return;
                }

                var subject = notification.Title;
                var htmlBody = BuildHtmlEmail(notification);
                var plainTextBody = notification.Message;

                await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email notification for notification {notification.NotificationId}");
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
        {
            // TODO: Implement actual email sending using SendGrid, AWS SES, etc.
            // For now, just log the email
            _logger.LogInformation($"EMAIL: To={toEmail}, Subject={subject}, Body={plainTextBody ?? htmlBody}");
            
            // Placeholder for actual implementation:
            /*
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_configuration["SendGrid:FromEmail"], "Wihngo");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody ?? htmlBody, htmlBody);
            var response = await client.SendEmailAsync(msg);
            */

            await Task.CompletedTask;
        }

        public async Task SendDailyDigestAsync(Guid userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return;

                // Get unread notifications from last 24 hours
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var notifications = await _db.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead && n.CreatedAt >= yesterday)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                if (!notifications.Any())
                {
                    _logger.LogInformation($"No notifications for daily digest for user {userId}");
                    return;
                }

                var subject = $"Your Daily Wihngo Digest - {notifications.Count} updates";
                var htmlBody = BuildDigestEmail(user, notifications);
                var plainTextBody = $"You have {notifications.Count} notifications waiting for you on Wihngo.";

                await SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending daily digest for user {userId}");
            }
        }

        private string BuildHtmlEmail(Notification notification)
        {
            var emoji = GetEmojiForNotificationType(notification.Type);
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{emoji} Wihngo Notification</h1>
        </div>
        <div class='content'>
            <h2>{notification.Title}</h2>
            <p>{notification.Message}</p>
            {(string.IsNullOrEmpty(notification.DeepLink) ? "" : $"<a href='https://wihngo.com{notification.DeepLink}' class='button'>View Details</a>")}
        </div>
        <div class='footer'>
            <p>You're receiving this because you have email notifications enabled for this type of update.</p>
            <p><a href='https://wihngo.com/settings/notifications'>Manage notification preferences</a></p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildDigestEmail(User user, System.Collections.Generic.List<Notification> notifications)
        {
            var notificationsList = string.Join("", notifications.Select(n => $@"
                <div style='border-bottom: 1px solid #ddd; padding: 15px 0;'>
                    <h3 style='margin: 0 0 5px 0;'>{GetEmojiForNotificationType(n.Type)} {n.Title}</h3>
                    <p style='margin: 5px 0; color: #666;'>{n.Message}</p>
                    <small style='color: #999;'>{n.CreatedAt:MMM dd, yyyy HH:mm}</small>
                </div>
            "));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Your Daily Wihngo Digest</h1>
        </div>
        <div class='content'>
            <p>Hi {user.Name},</p>
            <p>Here's what happened with your birds today:</p>
            {notificationsList}
            <a href='https://wihngo.com/notifications' class='button'>View All Notifications</a>
        </div>
        <div class='footer'>
            <p><a href='https://wihngo.com/settings/notifications'>Manage notification preferences</a></p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetEmojiForNotificationType(Models.Enums.NotificationType type)
        {
            return type switch
            {
                Models.Enums.NotificationType.BirdLoved => "??",
                Models.Enums.NotificationType.BirdSupported => "??",
                Models.Enums.NotificationType.CommentAdded => "??",
                Models.Enums.NotificationType.NewStory => "??",
                Models.Enums.NotificationType.HealthUpdate => "??",
                Models.Enums.NotificationType.BirdMemorial => "??",
                Models.Enums.NotificationType.NewFollower => "??",
                Models.Enums.NotificationType.MilestoneAchieved => "??",
                Models.Enums.NotificationType.BirdFeatured => "?",
                Models.Enums.NotificationType.PremiumExpiring => "??",
                Models.Enums.NotificationType.PaymentReceived => "?",
                Models.Enums.NotificationType.SecurityAlert => "??",
                Models.Enums.NotificationType.SuggestedBirds => "??",
                Models.Enums.NotificationType.ReEngagement => "??",
                _ => "??"
            };
        }
    }
}
