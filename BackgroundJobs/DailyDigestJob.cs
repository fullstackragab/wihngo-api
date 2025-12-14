namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Services.Interfaces;

    public class DailyDigestJob
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IEmailNotificationService _emailService;
        private readonly ILogger<DailyDigestJob> _logger;

        public DailyDigestJob(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IEmailNotificationService emailService,
            ILogger<DailyDigestJob> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send daily digest emails to users who have it enabled
        /// </summary>
        public async Task SendDailyDigestsAsync()
        {
            try
            {
                var currentHour = DateTime.UtcNow.Hour;
                
                // Get users who have daily digest enabled using raw SQL
                var sql = @"
                    SELECT user_id 
                    FROM notification_settings
                    WHERE enable_daily_digest = true 
                      AND EXTRACT(HOUR FROM daily_digest_time) = @CurrentHour";
                
                var usersForDigest = await _dbFactory.QueryListAsync<Guid>(sql, new { CurrentHour = currentHour });

                _logger.LogInformation($"Found {usersForDigest.Count} users for daily digest at hour {currentHour}");

                foreach (var userId in usersForDigest)
                {
                    try
                    {
                        await _emailService.SendDailyDigestAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending daily digest to user {userId}");
                    }
                }

                _logger.LogInformation($"Completed daily digest job for {usersForDigest.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily digest job");
            }
        }
    }
}
