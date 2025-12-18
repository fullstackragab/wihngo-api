using Dapper;
using Wihngo.Data;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service for managing bird follows.
    /// </summary>
    public class BirdFollowService : IBirdFollowService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<BirdFollowService> _logger;

        public BirdFollowService(
            IDbConnectionFactory dbFactory,
            ILogger<BirdFollowService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task FollowBirdAsync(Guid userId, Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if already following
            var existsSql = "SELECT 1 FROM user_bird_follows WHERE user_id = @UserId AND bird_id = @BirdId";
            var exists = await connection.QueryFirstOrDefaultAsync<int?>(existsSql, new { UserId = userId, BirdId = birdId });

            if (exists.HasValue)
            {
                _logger.LogDebug("User {UserId} is already following bird {BirdId}", userId, birdId);
                return;
            }

            // Insert follow
            var insertSql = @"
                INSERT INTO user_bird_follows (follow_id, user_id, bird_id, created_at)
                VALUES (@FollowId, @UserId, @BirdId, @CreatedAt)";

            await connection.ExecuteAsync(insertSql, new
            {
                FollowId = Guid.NewGuid(),
                UserId = userId,
                BirdId = birdId,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} followed bird {BirdId}", userId, birdId);
        }

        public async Task UnfollowBirdAsync(Guid userId, Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "DELETE FROM user_bird_follows WHERE user_id = @UserId AND bird_id = @BirdId";
            var deleted = await connection.ExecuteAsync(sql, new { UserId = userId, BirdId = birdId });

            if (deleted > 0)
            {
                _logger.LogInformation("User {UserId} unfollowed bird {BirdId}", userId, birdId);
            }
        }

        public async Task<bool> IsFollowingAsync(Guid userId, Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "SELECT 1 FROM user_bird_follows WHERE user_id = @UserId AND bird_id = @BirdId";
            var result = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { UserId = userId, BirdId = birdId });

            return result.HasValue;
        }

        public async Task<List<Guid>> GetFollowedBirdIdsAsync(Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "SELECT bird_id FROM user_bird_follows WHERE user_id = @UserId ORDER BY created_at DESC";
            var result = await connection.QueryAsync<Guid>(sql, new { UserId = userId });

            return result.ToList();
        }

        public async Task<int> GetFollowerCountAsync(Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "SELECT COUNT(*) FROM user_bird_follows WHERE bird_id = @BirdId";
            return await connection.ExecuteScalarAsync<int>(sql, new { BirdId = birdId });
        }
    }
}
