namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Services.Interfaces;

    public class NotificationCleanupJob
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NotificationCleanupJob> _logger;

        public NotificationCleanupJob(AppDbContext db, ILogger<NotificationCleanupJob> logger)
        {
            _db = db;
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
                
                var oldNotifications = await _db.Notifications
                    .Where(n => n.IsRead && n.ReadAt < cutoffDate)
                    .ToListAsync();

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
                
                var veryOldNotifications = await _db.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .ToListAsync();

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
