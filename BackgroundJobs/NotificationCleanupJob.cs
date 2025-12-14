namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Coravel.Invocable;
    using Dapper;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    public class NotificationCleanupJob
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<NotificationCleanupJob> _logger;

        public NotificationCleanupJob(
            AppDbContext db, 
            IDbConnectionFactory dbFactory,
            ILogger<NotificationCleanupJob> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Delete read notifications older than 30 days
        /// </summary>
        public async Task CleanupOldNotificationsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                
                // Use raw SQL to get old notifications
                var sql = @"
                    SELECT * FROM notifications
                    WHERE is_read = true AND read_at < @CutoffDate";
                
                var oldNotifications = await _dbFactory.QueryListAsync<Notification>(sql, new { CutoffDate = cutoffDate });

                if (oldNotifications.Any())
                {
                    _db.Notifications.RemoveRange(oldNotifications);
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation($"Cleaned up {oldNotifications.Count} old notifications");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old notifications");
            }
        }

        /// <summary>
        /// Delete unread notifications older than 90 days
        /// </summary>
        public async Task CleanupVeryOldNotificationsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                
                // Use raw SQL to get very old notifications
                var sql = @"
                    SELECT * FROM notifications
                    WHERE created_at < @CutoffDate";
                
                var veryOldNotifications = await _dbFactory.QueryListAsync<Notification>(sql, new { CutoffDate = cutoffDate });

                if (veryOldNotifications.Any())
                {
                    _db.Notifications.RemoveRange(veryOldNotifications);
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation($"Cleaned up {veryOldNotifications.Count} very old notifications");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up very old notifications");
            }
        }
    }
}
