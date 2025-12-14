namespace Wihngo.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Dapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    [Route("api/[controller]")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LikesController> _logger;

        public LikesController(
            IDbConnectionFactory dbFactory,
            INotificationService notificationService,
            ILogger<LikesController> logger)
        {
            _dbFactory = dbFactory;
            _notificationService = notificationService;
            _logger = logger;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        /// <summary>
        /// Get all likes for a story
        /// </summary>
        [HttpGet("story/{storyId}")]
        public async Task<ActionResult<PagedResult<StoryLikeDto>>> GetStoryLikes(
            Guid storyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if story exists
            var storyExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stories WHERE story_id = @StoryId)",
                new { StoryId = storyId });

            if (!storyExists)
                return NotFound(new { message = "Story not found" });

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM story_likes WHERE story_id = @StoryId",
                new { StoryId = storyId });

            // Get likes with user info
            var sql = @"
                SELECT 
                    sl.like_id,
                    sl.story_id,
                    sl.user_id,
                    sl.created_at,
                    u.name as user_name,
                    u.profile_image as user_profile_image
                FROM story_likes sl
                INNER JOIN users u ON sl.user_id = u.user_id
                WHERE sl.story_id = @StoryId
                ORDER BY sl.created_at DESC
                OFFSET @Offset LIMIT @Limit";

            var likes = await connection.QueryAsync<dynamic>(sql, new
            {
                StoryId = storyId,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            var items = new List<StoryLikeDto>();
            foreach (var like in likes)
            {
                items.Add(new StoryLikeDto
                {
                    LikeId = like.like_id,
                    StoryId = like.story_id,
                    UserId = like.user_id,
                    UserName = like.user_name ?? string.Empty,
                    UserProfileImage = like.user_profile_image,
                    CreatedAt = like.created_at
                });
            }

            var result = new PagedResult<StoryLikeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }

        /// <summary>
        /// Like a story
        /// </summary>
        [HttpPost("story")]
        [Authorize]
        public async Task<ActionResult<StoryLikeDto>> LikeStory([FromBody] StoryLikeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if story exists and get author
            var storySql = "SELECT author_id FROM stories WHERE story_id = @StoryId";
            var authorId = await connection.ExecuteScalarAsync<Guid?>(storySql, new { StoryId = dto.StoryId });

            if (authorId == null)
                return NotFound(new { message = "Story not found" });

            // Check if already liked
            var existingSql = @"
                SELECT like_id FROM story_likes 
                WHERE story_id = @StoryId AND user_id = @UserId";
            var existingLikeId = await connection.ExecuteScalarAsync<Guid?>(
                existingSql,
                new { StoryId = dto.StoryId, UserId = userId.Value });

            if (existingLikeId != null)
                return Conflict(new { message = "You have already liked this story", likeId = existingLikeId });

            // Create like
            var likeId = Guid.NewGuid();
            var insertSql = @"
                INSERT INTO story_likes (like_id, story_id, user_id, created_at)
                VALUES (@LikeId, @StoryId, @UserId, @CreatedAt)";

            var createdAt = DateTime.UtcNow;
            await connection.ExecuteAsync(insertSql, new
            {
                LikeId = likeId,
                StoryId = dto.StoryId,
                UserId = userId.Value,
                CreatedAt = createdAt
            });

            _logger.LogInformation("Story liked: {StoryId} by user {UserId}", dto.StoryId, userId.Value);

            // Send notification to story author (if not self-like)
            if (authorId.Value != userId.Value)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var userSql = "SELECT name FROM users WHERE user_id = @UserId";
                        var userName = await connection.QueryFirstOrDefaultAsync<string>(
                            userSql,
                            new { UserId = userId.Value });

                        if (!string.IsNullOrEmpty(userName))
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = authorId.Value,
                                Type = NotificationType.BirdLoved,
                                Title = "New like",
                                Message = $"{userName} liked your story",
                                Priority = NotificationPriority.Low,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/story/{dto.StoryId}",
                                StoryId = dto.StoryId,
                                ActorUserId = userId.Value
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send like notification");
                    }
                });
            }

            // Get user info for response
            var userInfoSql = "SELECT name, profile_image FROM users WHERE user_id = @UserId";
            var userInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                userInfoSql,
                new { UserId = userId.Value });

            return CreatedAtAction(
                nameof(GetStoryLikes),
                new { storyId = dto.StoryId },
                new StoryLikeDto
                {
                    LikeId = likeId,
                    StoryId = dto.StoryId,
                    UserId = userId.Value,
                    UserName = userInfo?.name ?? string.Empty,
                    UserProfileImage = userInfo?.profile_image,
                    CreatedAt = createdAt
                });
        }

        /// <summary>
        /// Unlike a story
        /// </summary>
        [HttpDelete("story/{storyId}")]
        [Authorize]
        public async Task<IActionResult> UnlikeStory(Guid storyId)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if like exists
            var checkSql = @"
                SELECT like_id FROM story_likes 
                WHERE story_id = @StoryId AND user_id = @UserId";
            var likeId = await connection.ExecuteScalarAsync<Guid?>(
                checkSql,
                new { StoryId = storyId, UserId = userId.Value });

            if (likeId == null)
                return NotFound(new { message = "Like not found" });

            // Delete like
            var deleteSql = "DELETE FROM story_likes WHERE like_id = @LikeId";
            await connection.ExecuteAsync(deleteSql, new { LikeId = likeId.Value });

            _logger.LogInformation("Story unliked: {StoryId} by user {UserId}", storyId, userId.Value);

            return NoContent();
        }

        /// <summary>
        /// Check if current user has liked a story
        /// </summary>
        [HttpGet("story/{storyId}/check")]
        [Authorize]
        public async Task<ActionResult<object>> CheckStoryLike(Guid storyId)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = @"
                SELECT like_id, created_at 
                FROM story_likes 
                WHERE story_id = @StoryId AND user_id = @UserId";
            
            var like = await connection.QueryFirstOrDefaultAsync<dynamic>(
                sql,
                new { StoryId = storyId, UserId = userId.Value });

            if (like == null)
            {
                return Ok(new { isLiked = false, likeId = (Guid?)null, createdAt = (DateTime?)null });
            }

            return Ok(new 
            { 
                isLiked = true, 
                likeId = (Guid)like.like_id, 
                createdAt = (DateTime)like.created_at 
            });
        }

        /// <summary>
        /// Get stories liked by current user
        /// </summary>
        [HttpGet("my-likes")]
        [Authorize]
        public async Task<ActionResult<PagedResult<StoryLikeDto>>> GetMyLikes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM story_likes WHERE user_id = @UserId",
                new { UserId = userId.Value });

            // Get likes
            var sql = @"
                SELECT 
                    sl.like_id,
                    sl.story_id,
                    sl.user_id,
                    sl.created_at,
                    u.name as user_name,
                    u.profile_image as user_profile_image
                FROM story_likes sl
                INNER JOIN users u ON sl.user_id = u.user_id
                WHERE sl.user_id = @UserId
                ORDER BY sl.created_at DESC
                OFFSET @Offset LIMIT @Limit";

            var likes = await connection.QueryAsync<dynamic>(sql, new
            {
                UserId = userId.Value,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            var items = new List<StoryLikeDto>();
            foreach (var like in likes)
            {
                items.Add(new StoryLikeDto
                {
                    LikeId = like.like_id,
                    StoryId = like.story_id,
                    UserId = like.user_id,
                    UserName = like.user_name ?? string.Empty,
                    UserProfileImage = like.user_profile_image,
                    CreatedAt = like.created_at
                });
            }

            var result = new PagedResult<StoryLikeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }
    }
}
