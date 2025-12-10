namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    public class PremiumExpiryNotificationJob
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PremiumExpiryNotificationJob> _logger;

        public PremiumExpiryNotificationJob(
            AppDbContext db,
            INotificationService notificationService,
            ILogger<PremiumExpiryNotificationJob> logger)
        {
            _db = db;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Check for premium subscriptions expiring in 7 days and send notifications
        /// </summary>
        public async Task CheckExpiringPremiumAsync()
        {
            try
            {
                var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
                var eightDaysFromNow = DateTime.UtcNow.AddDays(8);

                // Find subscriptions expiring in 7 days (within 24-hour window)
                var expiringSubscriptions = await _db.BirdPremiumSubscriptions
                    .Where(s => s.Status == "active" && 
                                s.CurrentPeriodEnd >= sevenDaysFromNow &&
                                s.CurrentPeriodEnd < eightDaysFromNow &&
                                s.Plan != "lifetime")
                    .Include(s => s.Bird)
                    .ToListAsync();

                _logger.LogInformation($"Found {expiringSubscriptions.Count} expiring premium subscriptions");

                foreach (var subscription in expiringSubscriptions)
                {
                    try
                    {
                        // Check if we already sent notification for this subscription
                        var alreadyNotified = await _db.Notifications
                            .AnyAsync(n => n.UserId == subscription.OwnerId &&
                                          n.Type == NotificationType.PremiumExpiring &&
                                          n.BirdId == subscription.BirdId &&
                                          n.CreatedAt >= DateTime.UtcNow.AddDays(-8));

                        if (alreadyNotified)
                            continue;

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = subscription.OwnerId,
                            Type = NotificationType.PremiumExpiring,
                            Title = "?? Premium expires in 7 days",
                            Message = $"Your premium subscription for {subscription.Bird?.Name ?? "your bird"} expires on {subscription.CurrentPeriodEnd:MMM dd}. Renew now to keep your premium features!",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = "/premium",
                            BirdId = subscription.BirdId
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending expiry notification for subscription {subscription.Id}");
                    }
                }

                _logger.LogInformation($"Sent premium expiry notifications for {expiringSubscriptions.Count} subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in premium expiry notification job");
            }
        }
    }
}
