namespace Wihngo.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    public class CommentsController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            IDbConnectionFactory dbFactory,
            INotificationService notificationService,
            ILogger<CommentsController> logger)
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
        /// Get all comments for a story (top-level only, use /replies endpoint for nested)
        /// </summary>
        [HttpGet("story/{storyId}")]
        public async Task<ActionResult<PagedResult<CommentDto>>> GetStoryComments(
            Guid storyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var currentUserId = GetUserIdClaim();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if story exists
            var storyExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stories WHERE story_id = @StoryId)",
                new { StoryId = storyId });

            if (!storyExists)
                return NotFound(new { message = "Story not found" });

            // Get total count (only top-level comments)
            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM comments WHERE story_id = @StoryId AND parent_comment_id IS NULL",
                new { StoryId = storyId });

            // Get comments with user info and reply count
            var sql = @"
                SELECT 
                    c.comment_id,
                    c.story_id,
                    c.user_id,
                    c.content,
                    c.created_at,
                    c.updated_at,
                    c.parent_comment_id,
                    c.like_count,
                    u.name as user_name,
                    u.profile_image as user_profile_image,
                    (SELECT COUNT(*) FROM comments WHERE parent_comment_id = c.comment_id) as reply_count,
                    CASE WHEN @CurrentUserId IS NOT NULL THEN
                        EXISTS(SELECT 1 FROM comment_likes WHERE comment_id = c.comment_id AND user_id = @CurrentUserId)
                    ELSE false END as is_liked_by_current_user
                FROM comments c
                INNER JOIN users u ON c.user_id = u.user_id
                WHERE c.story_id = @StoryId AND c.parent_comment_id IS NULL
                ORDER BY c.created_at DESC
                OFFSET @Offset LIMIT @Limit";

            var comments = await connection.QueryAsync<dynamic>(sql, new
            {
                StoryId = storyId,
                CurrentUserId = currentUserId,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            var items = new List<CommentDto>();
            foreach (var comment in comments)
            {
                items.Add(new CommentDto
                {
                    CommentId = comment.comment_id,
                    StoryId = comment.story_id,
                    UserId = comment.user_id,
                    UserName = comment.user_name ?? string.Empty,
                    UserProfileImage = comment.user_profile_image,
                    Content = comment.content ?? string.Empty,
                    CreatedAt = comment.created_at,
                    UpdatedAt = comment.updated_at,
                    ParentCommentId = comment.parent_comment_id,
                    LikeCount = comment.like_count,
                    IsLikedByCurrentUser = comment.is_liked_by_current_user,
                    ReplyCount = comment.reply_count
                });
            }

            var result = new PagedResult<CommentDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }

        /// <summary>
        /// Get a single comment with all its replies
        /// </summary>
        [HttpGet("{commentId}")]
        public async Task<ActionResult<CommentWithRepliesDto>> GetComment(Guid commentId)
        {
            var currentUserId = GetUserIdClaim();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get main comment
            var commentSql = @"
                SELECT 
                    c.comment_id,
                    c.story_id,
                    c.user_id,
                    c.content,
                    c.created_at,
                    c.updated_at,
                    c.parent_comment_id,
                    c.like_count,
                    u.name as user_name,
                    u.profile_image as user_profile_image,
                    CASE WHEN @CurrentUserId IS NOT NULL THEN
                        EXISTS(SELECT 1 FROM comment_likes WHERE comment_id = c.comment_id AND user_id = @CurrentUserId)
                    ELSE false END as is_liked_by_current_user
                FROM comments c
                INNER JOIN users u ON c.user_id = u.user_id
                WHERE c.comment_id = @CommentId";

            var comment = await connection.QueryFirstOrDefaultAsync<dynamic>(
                commentSql,
                new { CommentId = commentId, CurrentUserId = currentUserId });

            if (comment == null)
                return NotFound(new { message = "Comment not found" });

            // Get replies
            var repliesSql = @"
                SELECT 
                    c.comment_id,
                    c.story_id,
                    c.user_id,
                    c.content,
                    c.created_at,
                    c.updated_at,
                    c.parent_comment_id,
                    c.like_count,
                    u.name as user_name,
                    u.profile_image as user_profile_image,
                    (SELECT COUNT(*) FROM comments WHERE parent_comment_id = c.comment_id) as reply_count,
                    CASE WHEN @CurrentUserId IS NOT NULL THEN
                        EXISTS(SELECT 1 FROM comment_likes WHERE comment_id = c.comment_id AND user_id = @CurrentUserId)
                    ELSE false END as is_liked_by_current_user
                FROM comments c
                INNER JOIN users u ON c.user_id = u.user_id
                WHERE c.parent_comment_id = @CommentId
                ORDER BY c.created_at ASC";

            var replies = await connection.QueryAsync<dynamic>(
                repliesSql,
                new { CommentId = commentId, CurrentUserId = currentUserId });

            var replyDtos = new List<CommentDto>();
            foreach (var reply in replies)
            {
                replyDtos.Add(new CommentDto
                {
                    CommentId = reply.comment_id,
                    StoryId = reply.story_id,
                    UserId = reply.user_id,
                    UserName = reply.user_name ?? string.Empty,
                    UserProfileImage = reply.user_profile_image,
                    Content = reply.content ?? string.Empty,
                    CreatedAt = reply.created_at,
                    UpdatedAt = reply.updated_at,
                    ParentCommentId = reply.parent_comment_id,
                    LikeCount = reply.like_count,
                    IsLikedByCurrentUser = reply.is_liked_by_current_user,
                    ReplyCount = reply.reply_count
                });
            }

            var result = new CommentWithRepliesDto
            {
                CommentId = comment.comment_id,
                StoryId = comment.story_id,
                UserId = comment.user_id,
                UserName = comment.user_name ?? string.Empty,
                UserProfileImage = comment.user_profile_image,
                Content = comment.content ?? string.Empty,
                CreatedAt = comment.created_at,
                UpdatedAt = comment.updated_at,
                ParentCommentId = comment.parent_comment_id,
                LikeCount = comment.like_count,
                IsLikedByCurrentUser = comment.is_liked_by_current_user,
                Replies = replyDtos
            };

            return Ok(result);
        }

        /// <summary>
        /// Get replies for a comment
        /// </summary>
        [HttpGet("{commentId}/replies")]
        public async Task<ActionResult<PagedResult<CommentDto>>> GetCommentReplies(
            Guid commentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var currentUserId = GetUserIdClaim();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if parent comment exists
            var parentExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM comments WHERE comment_id = @CommentId)",
                new { CommentId = commentId });

            if (!parentExists)
                return NotFound(new { message = "Parent comment not found" });

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM comments WHERE parent_comment_id = @CommentId",
                new { CommentId = commentId });

            // Get replies
            var sql = @"
                SELECT 
                    c.comment_id,
                    c.story_id,
                    c.user_id,
                    c.content,
                    c.created_at,
                    c.updated_at,
                    c.parent_comment_id,
                    c.like_count,
                    u.name as user_name,
                    u.profile_image as user_profile_image,
                    (SELECT COUNT(*) FROM comments WHERE parent_comment_id = c.comment_id) as reply_count,
                    CASE WHEN @CurrentUserId IS NOT NULL THEN
                        EXISTS(SELECT 1 FROM comment_likes WHERE comment_id = c.comment_id AND user_id = @CurrentUserId)
                    ELSE false END as is_liked_by_current_user
                FROM comments c
                INNER JOIN users u ON c.user_id = u.user_id
                WHERE c.parent_comment_id = @CommentId
                ORDER BY c.created_at ASC
                OFFSET @Offset LIMIT @Limit";

            var replies = await connection.QueryAsync<dynamic>(sql, new
            {
                CommentId = commentId,
                CurrentUserId = currentUserId,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            var items = new List<CommentDto>();
            foreach (var reply in replies)
            {
                items.Add(new CommentDto
                {
                    CommentId = reply.comment_id,
                    StoryId = reply.story_id,
                    UserId = reply.user_id,
                    UserName = reply.user_name ?? string.Empty,
                    UserProfileImage = reply.user_profile_image,
                    Content = reply.content ?? string.Empty,
                    CreatedAt = reply.created_at,
                    UpdatedAt = reply.updated_at,
                    ParentCommentId = reply.parent_comment_id,
                    LikeCount = reply.like_count,
                    IsLikedByCurrentUser = reply.is_liked_by_current_user,
                    ReplyCount = reply.reply_count
                });
            }

            var result = new PagedResult<CommentDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }

        /// <summary>
        /// Create a new comment
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CommentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if story exists and get author
            var storySql = "SELECT author_id FROM stories WHERE story_id = @StoryId";
            var storyAuthorId = await connection.ExecuteScalarAsync<Guid?>(
                storySql,
                new { StoryId = dto.StoryId });

            if (storyAuthorId == null)
                return NotFound(new { message = "Story not found" });

            // If replying, check parent comment exists and belongs to same story
            if (dto.ParentCommentId.HasValue)
            {
                var parentSql = "SELECT story_id, user_id FROM comments WHERE comment_id = @CommentId";
                var parent = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    parentSql,
                    new { CommentId = dto.ParentCommentId.Value });

                if (parent == null)
                    return NotFound(new { message = "Parent comment not found" });

                if ((Guid)parent.story_id != dto.StoryId)
                    return BadRequest(new { message = "Parent comment does not belong to this story" });
            }

            // Create comment
            var commentId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var insertSql = @"
                INSERT INTO comments (comment_id, story_id, user_id, content, created_at, parent_comment_id, like_count)
                VALUES (@CommentId, @StoryId, @UserId, @Content, @CreatedAt, @ParentCommentId, 0)";

            await connection.ExecuteAsync(insertSql, new
            {
                CommentId = commentId,
                StoryId = dto.StoryId,
                UserId = userId.Value,
                Content = dto.Content.Trim(),
                CreatedAt = createdAt,
                ParentCommentId = dto.ParentCommentId
            });

            _logger.LogInformation("Comment created: {CommentId} on story {StoryId} by user {UserId}",
                commentId, dto.StoryId, userId.Value);

            // Send notification to story author (if not self-comment and not a reply)
            if (storyAuthorId.Value != userId.Value && !dto.ParentCommentId.HasValue)
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
                                UserId = storyAuthorId.Value,
                                Type = NotificationType.CommentAdded,
                                Title = "New comment",
                                Message = $"{userName} commented on your story",
                                Priority = NotificationPriority.Medium,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/story/{dto.StoryId}",
                                StoryId = dto.StoryId,
                                ActorUserId = userId.Value
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send comment notification");
                    }
                });
            }

            // If replying, send notification to parent comment author
            if (dto.ParentCommentId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var parentUserSql = "SELECT user_id FROM comments WHERE comment_id = @CommentId";
                        var parentUserId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                            parentUserSql,
                            new { CommentId = dto.ParentCommentId.Value });

                        // Only notify if not replying to self
                        if (parentUserId.HasValue && parentUserId.Value != userId.Value)
                        {
                            var userSql = "SELECT name FROM users WHERE user_id = @UserId";
                            var userName = await connection.QueryFirstOrDefaultAsync<string>(
                                userSql,
                                new { UserId = userId.Value });

                            if (!string.IsNullOrEmpty(userName))
                            {
                                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                                {
                                    UserId = parentUserId.Value,
                                    Type = NotificationType.CommentAdded,
                                    Title = "New reply",
                                    Message = $"{userName} replied to your comment",
                                    Priority = NotificationPriority.Medium,
                                    Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                    DeepLink = $"/story/{dto.StoryId}",
                                    StoryId = dto.StoryId,
                                    ActorUserId = userId.Value
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send reply notification");
                    }
                });
            }

            // Get user info for response
            var userInfoSql = "SELECT name, profile_image FROM users WHERE user_id = @UserId";
            var userInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                userInfoSql,
                new { UserId = userId.Value });

            var result = new CommentDto
            {
                CommentId = commentId,
                StoryId = dto.StoryId,
                UserId = userId.Value,
                UserName = userInfo?.name ?? string.Empty,
                UserProfileImage = userInfo?.profile_image,
                Content = dto.Content.Trim(),
                CreatedAt = createdAt,
                UpdatedAt = null,
                ParentCommentId = dto.ParentCommentId,
                LikeCount = 0,
                IsLikedByCurrentUser = false,
                ReplyCount = 0
            };

            return CreatedAtAction(nameof(GetComment), new { commentId }, result);
        }

        /// <summary>
        /// Update a comment
        /// </summary>
        [HttpPut("{commentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] CommentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if comment exists and user is owner
            var checkSql = "SELECT user_id FROM comments WHERE comment_id = @CommentId";
            var commentUserId = await connection.ExecuteScalarAsync<Guid?>(
                checkSql,
                new { CommentId = commentId });

            if (commentUserId == null)
                return NotFound(new { message = "Comment not found" });

            if (commentUserId.Value != userId.Value)
                return Forbid();

            // Update comment
            var updateSql = @"
                UPDATE comments 
                SET content = @Content, updated_at = @UpdatedAt 
                WHERE comment_id = @CommentId";

            await connection.ExecuteAsync(updateSql, new
            {
                Content = dto.Content.Trim(),
                UpdatedAt = DateTime.UtcNow,
                CommentId = commentId
            });

            _logger.LogInformation("Comment updated: {CommentId} by user {UserId}", commentId, userId.Value);

            return NoContent();
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if comment exists and user is owner
            var checkSql = "SELECT user_id FROM comments WHERE comment_id = @CommentId";
            var commentUserId = await connection.ExecuteScalarAsync<Guid?>(
                checkSql,
                new { CommentId = commentId });

            if (commentUserId == null)
                return NotFound(new { message = "Comment not found" });

            if (commentUserId.Value != userId.Value)
                return Forbid();

            // Delete comment (cascade will delete replies and likes)
            var deleteSql = "DELETE FROM comments WHERE comment_id = @CommentId";
            await connection.ExecuteAsync(deleteSql, new { CommentId = commentId });

            _logger.LogInformation("Comment deleted: {CommentId} by user {UserId}", commentId, userId.Value);

            return NoContent();
        }

        // ===== COMMENT LIKES =====

        /// <summary>
        /// Get all likes for a comment
        /// </summary>
        [HttpGet("{commentId}/likes")]
        public async Task<ActionResult<PagedResult<CommentLikeDto>>> GetCommentLikes(
            Guid commentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if comment exists
            var commentExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM comments WHERE comment_id = @CommentId)",
                new { CommentId = commentId });

            if (!commentExists)
                return NotFound(new { message = "Comment not found" });

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM comment_likes WHERE comment_id = @CommentId",
                new { CommentId = commentId });

            // Get likes with user info
            var sql = @"
                SELECT 
                    cl.like_id,
                    cl.comment_id,
                    cl.user_id,
                    cl.created_at,
                    u.name as user_name,
                    u.profile_image as user_profile_image
                FROM comment_likes cl
                INNER JOIN users u ON cl.user_id = u.user_id
                WHERE cl.comment_id = @CommentId
                ORDER BY cl.created_at DESC
                OFFSET @Offset LIMIT @Limit";

            var likes = await connection.QueryAsync<dynamic>(sql, new
            {
                CommentId = commentId,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            var items = new List<CommentLikeDto>();
            foreach (var like in likes)
            {
                items.Add(new CommentLikeDto
                {
                    LikeId = like.like_id,
                    CommentId = like.comment_id,
                    UserId = like.user_id,
                    UserName = like.user_name ?? string.Empty,
                    UserProfileImage = like.user_profile_image,
                    CreatedAt = like.created_at
                });
            }

            var result = new PagedResult<CommentLikeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }

        /// <summary>
        /// Like a comment
        /// </summary>
        [HttpPost("{commentId}/like")]
        [Authorize]
        public async Task<ActionResult<CommentLikeDto>> LikeComment(Guid commentId)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if comment exists and get author
            var commentSql = "SELECT user_id FROM comments WHERE comment_id = @CommentId";
            var commentAuthorId = await connection.ExecuteScalarAsync<Guid?>(
                commentSql,
                new { CommentId = commentId });

            if (commentAuthorId == null)
                return NotFound(new { message = "Comment not found" });

            // Check if already liked
            var existingSql = @"
                SELECT like_id FROM comment_likes 
                WHERE comment_id = @CommentId AND user_id = @UserId";
            var existingLikeId = await connection.ExecuteScalarAsync<Guid?>(
                existingSql,
                new { CommentId = commentId, UserId = userId.Value });

            if (existingLikeId != null)
                return Conflict(new { message = "You have already liked this comment", likeId = existingLikeId });

            // Create like
            var likeId = Guid.NewGuid();
            var insertSql = @"
                INSERT INTO comment_likes (like_id, comment_id, user_id, created_at)
                VALUES (@LikeId, @CommentId, @UserId, @CreatedAt)";

            var createdAt = DateTime.UtcNow;
            await connection.ExecuteAsync(insertSql, new
            {
                LikeId = likeId,
                CommentId = commentId,
                UserId = userId.Value,
                CreatedAt = createdAt
            });

            _logger.LogInformation("Comment liked: {CommentId} by user {UserId}", commentId, userId.Value);

            // Send notification to comment author (if not self-like)
            if (commentAuthorId.Value != userId.Value)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var userSql = "SELECT name FROM users WHERE user_id = @UserId";
                        var userName = await connection.QueryFirstOrDefaultAsync<string>(
                            userSql,
                            new { UserId = userId.Value });

                        var storySql = "SELECT story_id FROM comments WHERE comment_id = @CommentId";
                        var storyId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                            storySql,
                            new { CommentId = commentId });

                        if (!string.IsNullOrEmpty(userName) && storyId.HasValue)
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = commentAuthorId.Value,
                                Type = NotificationType.BirdLoved,
                                Title = "Comment liked",
                                Message = $"{userName} liked your comment",
                                Priority = NotificationPriority.Low,
                                Channels = NotificationChannel.InApp,
                                DeepLink = $"/story/{storyId.Value}",
                                StoryId = storyId.Value,
                                ActorUserId = userId.Value
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send comment like notification");
                    }
                });
            }

            // Get user info for response
            var userInfoSql = "SELECT name, profile_image FROM users WHERE user_id = @UserId";
            var userInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                userInfoSql,
                new { UserId = userId.Value });

            return CreatedAtAction(
                nameof(GetCommentLikes),
                new { commentId },
                new CommentLikeDto
                {
                    LikeId = likeId,
                    CommentId = commentId,
                    UserId = userId.Value,
                    UserName = userInfo?.name ?? string.Empty,
                    UserProfileImage = userInfo?.profile_image,
                    CreatedAt = createdAt
                });
        }

        /// <summary>
        /// Unlike a comment
        /// </summary>
        [HttpDelete("{commentId}/like")]
        [Authorize]
        public async Task<IActionResult> UnlikeComment(Guid commentId)
        {
            var userId = GetUserIdClaim();
            if (userId == null)
                return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if like exists
            var checkSql = @"
                SELECT like_id FROM comment_likes 
                WHERE comment_id = @CommentId AND user_id = @UserId";
            var likeId = await connection.ExecuteScalarAsync<Guid?>(
                checkSql,
                new { CommentId = commentId, UserId = userId.Value });

            if (likeId == null)
                return NotFound(new { message = "Like not found" });

            // Delete like
            var deleteSql = "DELETE FROM comment_likes WHERE like_id = @LikeId";
            await connection.ExecuteAsync(deleteSql, new { LikeId = likeId.Value });

            _logger.LogInformation("Comment unliked: {CommentId} by user {UserId}", commentId, userId.Value);

            return NoContent();
        }
    }
}
