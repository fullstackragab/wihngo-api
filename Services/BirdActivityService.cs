using System;
using System.Threading.Tasks;
using Dapper;
using Wihngo.Data;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class BirdActivityService : IBirdActivityService
    {
        private readonly IDbConnectionFactory _dbFactory;

        // Activity thresholds in days
        private const int ActiveThresholdDays = 30;
        private const int QuietThresholdDays = 90;

        public BirdActivityService(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public BirdActivityStatus GetActivityStatus(DateTime? lastActivityAt, bool isMemorial)
        {
            if (isMemorial)
            {
                return BirdActivityStatus.Memorial;
            }

            if (!lastActivityAt.HasValue)
            {
                // No activity recorded - treat as inactive
                return BirdActivityStatus.Inactive;
            }

            var daysSinceActivity = (DateTime.UtcNow - lastActivityAt.Value).TotalDays;

            if (daysSinceActivity <= ActiveThresholdDays)
            {
                return BirdActivityStatus.Active;
            }
            else if (daysSinceActivity <= QuietThresholdDays)
            {
                return BirdActivityStatus.Quiet;
            }
            else
            {
                return BirdActivityStatus.Inactive;
            }
        }

        public bool CanReceiveSupport(BirdActivityStatus status)
        {
            return status == BirdActivityStatus.Active || status == BirdActivityStatus.Quiet;
        }

        public string GetLastSeenText(DateTime? lastActivityAt, bool isMemorial)
        {
            if (isMemorial)
            {
                return "Remembered with love";
            }

            if (!lastActivityAt.HasValue)
            {
                return "No recent activity";
            }

            var daysSinceActivity = (DateTime.UtcNow - lastActivityAt.Value).TotalDays;

            if (daysSinceActivity < 1)
            {
                return "Recently active";
            }
            else if (daysSinceActivity < 7)
            {
                return "Active this week";
            }
            else if (daysSinceActivity < 14)
            {
                return "Active recently";
            }
            else if (daysSinceActivity < 30)
            {
                return $"Last seen: {(int)Math.Floor(daysSinceActivity / 7)} weeks ago";
            }
            else if (daysSinceActivity < 60)
            {
                return "Last seen: about a month ago";
            }
            else if (daysSinceActivity < 365)
            {
                var months = (int)Math.Floor(daysSinceActivity / 30);
                return $"Last seen: {months} month{(months > 1 ? "s" : "")} ago";
            }
            else
            {
                var years = (int)Math.Floor(daysSinceActivity / 365);
                return $"Last seen: {years} year{(years > 1 ? "s" : "")} ago";
            }
        }

        public string? GetSupportUnavailableMessage(BirdActivityStatus status)
        {
            return status switch
            {
                BirdActivityStatus.Active => null,
                BirdActivityStatus.Quiet => null,
                BirdActivityStatus.Inactive => "Support is currently unavailable for this bird.",
                BirdActivityStatus.Memorial => "Support is closed for this bird.",
                _ => null
            };
        }

        public async Task UpdateLastActivityAsync(Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            await connection.ExecuteAsync(
                "UPDATE birds SET last_activity_at = @Now WHERE bird_id = @BirdId",
                new { BirdId = birdId, Now = DateTime.UtcNow });
        }
    }
}
