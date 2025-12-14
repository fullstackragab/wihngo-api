namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Coravel.Invocable;
    using Dapper;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    public class PremiumExpiryNotificationJob
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PremiumExpiryNotificationJob> _logger;

        public PremiumExpiryNotificationJob(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            INotificationService notificationService,
            ILogger<PremiumExpiryNotificationJob> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
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

                // Use raw SQL with JOIN to get expiring subscriptions with bird info
                var sql = @"
                    SELECT 
                        s.*,
                        b.bird_id, b.name, b.owner_id
                    FROM bird_premium_subscriptions s
                    JOIN birds b ON s.bird_id = b.bird_id
                    WHERE s.status = 'active' 
                      AND s.current_period_end >= @SevenDays
                      AND s.current_period_end < @EightDays
                      AND s.plan != 'lifetime'";

                var connection = await _dbFactory.CreateOpenConnectionAsync();
                try
                {
                    var expiringSubscriptions = new List<BirdPremiumSubscription>();
                    
                    await connection.QueryAsync<BirdPremiumSubscription, Bird, BirdPremiumSubscription>(
                        sql,
                        (subscription, bird) =>
                        {
                            subscription.Bird = bird;
                            expiringSubscriptions.Add(subscription);
                            return subscription;
                        },
                        new { SevenDays = sevenDaysFromNow, EightDays = eightDaysFromNow },
                        splitOn: "bird_id");

                    _logger.LogInformation($"Found {expiringSubscriptions.Count} expiring premium subscriptions");

                    foreach (var subscription in expiringSubscriptions)
                    {
                        try
                        {
                            // Check if we already sent notification
                            var checkSql = @"
                                SELECT EXISTS(
                                    SELECT 1 FROM notifications
                                    WHERE user_id = @UserId 
                                      AND type = @Type
                                      AND bird_id = @BirdId
                                      AND created_at >= @Since
                                )";
                            
                            var alreadyNotified = await connection.ExecuteScalarAsync<bool>(checkSql, new
                            {
                                UserId = subscription.OwnerId,
                                Type = NotificationType.PremiumExpiring.ToString(),
                                BirdId = subscription.BirdId,
                                Since = DateTime.UtcNow.AddDays(-8)
                            });

                            if (alreadyNotified)
                                continue;

                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = subscription.OwnerId,
                                Type = NotificationType.PremiumExpiring,
                                Title = "? Premium expires in 7 days",
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
                finally
                {
                    await connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in premium expiry notification job");
            }
        }
    }
}
