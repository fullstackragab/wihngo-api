using System.Text.RegularExpressions;
using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service for managing Kind Words - a constrained, care-focused comment system.
    /// </summary>
    public class KindWordsService : IKindWordsService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<KindWordsService> _logger;

        // Rate limit: 3 kind words per bird per day per user
        private const int MaxKindWordsPerBirdPerDay = 3;
        private const int MaxTextLength = 200;

        // URL detection regex
        private static readonly Regex UrlRegex = new Regex(
            @"(https?://|www\.|[a-z0-9-]+\.(com|org|net|io|app|co|me|info|biz))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public KindWordsService(
            IDbConnectionFactory dbFactory,
            ILogger<KindWordsService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<KindWordsSectionDto> GetKindWordsSectionAsync(
            Guid birdId, Guid? currentUserId, int page = 1, int pageSize = 20)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get bird info including kind_words_enabled
            var birdSql = @"
                SELECT bird_id, name, owner_id as owner_user_id, kind_words_enabled
                FROM birds
                WHERE bird_id = @BirdId";
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = birdId });

            if (bird == null)
            {
                return new KindWordsSectionDto
                {
                    IsEnabled = false,
                    CanPost = false,
                    CannotPostReason = "Bird not found",
                    BirdName = "",
                    KindWords = new List<KindWordDto>(),
                    TotalCount = 0
                };
            }

            bool isEnabled = bird.kind_words_enabled ?? true;
            string birdName = bird.name ?? "";

            // Check if current user can post
            bool canPost = false;
            string? cannotPostReason = null;

            if (currentUserId.HasValue && isEnabled)
            {
                var (canPostResult, reason) = await CanUserPostAsync(birdId, currentUserId.Value);
                canPost = canPostResult;
                cannotPostReason = reason;
            }
            else if (!isEnabled)
            {
                cannotPostReason = "Kind words are currently disabled for this bird";
            }
            else
            {
                cannotPostReason = "Sign in to leave a kind word";
            }

            // Get kind words (newest first)
            var offset = (page - 1) * pageSize;
            var kindWordsSql = @"
                SELECT kw.id, kw.bird_id, kw.author_user_id, kw.text, kw.created_at,
                       u.name as author_name, u.profile_image as author_profile_image
                FROM kind_words kw
                JOIN users u ON kw.author_user_id = u.user_id
                WHERE kw.bird_id = @BirdId
                  AND kw.is_deleted = false
                  AND kw.is_visible = true
                ORDER BY kw.created_at DESC
                LIMIT @PageSize OFFSET @Offset";

            var kindWords = await connection.QueryAsync<dynamic>(kindWordsSql, new
            {
                BirdId = birdId,
                PageSize = pageSize,
                Offset = offset
            });

            // Get total count
            var countSql = @"
                SELECT COUNT(*)
                FROM kind_words
                WHERE bird_id = @BirdId
                  AND is_deleted = false
                  AND is_visible = true";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { BirdId = birdId });

            return new KindWordsSectionDto
            {
                IsEnabled = isEnabled,
                CanPost = canPost,
                CannotPostReason = canPost ? null : cannotPostReason,
                BirdName = birdName,
                TotalCount = totalCount,
                KindWords = kindWords.Select(kw => new KindWordDto
                {
                    Id = kw.id,
                    BirdId = kw.bird_id,
                    AuthorUserId = kw.author_user_id,
                    AuthorName = kw.author_name ?? "Anonymous",
                    AuthorProfileImage = kw.author_profile_image,
                    Text = kw.text,
                    CreatedAt = kw.created_at
                }).ToList()
            };
        }

        public async Task<KindWordResultDto> PostKindWordAsync(Guid birdId, Guid authorUserId, KindWordCreateDto dto)
        {
            // Validate text
            var (isValid, validationError) = ValidateText(dto.Text);
            if (!isValid)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = validationError
                };
            }

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists and kind words are enabled
            var birdSql = @"
                SELECT bird_id, name, owner_id as owner_user_id, kind_words_enabled
                FROM birds
                WHERE bird_id = @BirdId";
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = birdId });

            if (bird == null)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Bird not found"
                };
            }

            if (!(bird.kind_words_enabled ?? true))
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Kind words are currently disabled for this bird"
                };
            }

            // Check user eligibility
            var (canPost, reason) = await CanUserPostInternalAsync(connection, birdId, authorUserId);
            if (!canPost)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = reason ?? "You cannot post kind words on this bird"
                };
            }

            // Check rate limit
            var rateLimitSql = @"
                SELECT COUNT(*)
                FROM kind_words
                WHERE bird_id = @BirdId
                  AND author_user_id = @AuthorUserId
                  AND created_at > @Since
                  AND is_deleted = false";
            var recentCount = await connection.ExecuteScalarAsync<int>(rateLimitSql, new
            {
                BirdId = birdId,
                AuthorUserId = authorUserId,
                Since = DateTime.UtcNow.AddDays(-1)
            });

            if (recentCount >= MaxKindWordsPerBirdPerDay)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "You've shared enough kind words for today. Come back tomorrow!"
                };
            }

            // Insert kind word
            var kindWordId = Guid.NewGuid();
            var insertSql = @"
                INSERT INTO kind_words (id, bird_id, author_user_id, text, created_at, is_deleted, is_visible)
                VALUES (@Id, @BirdId, @AuthorUserId, @Text, @CreatedAt, false, true)";

            await connection.ExecuteAsync(insertSql, new
            {
                Id = kindWordId,
                BirdId = birdId,
                AuthorUserId = authorUserId,
                Text = dto.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            // Get author info for response
            var authorSql = "SELECT name, profile_image FROM users WHERE user_id = @UserId";
            var author = await connection.QueryFirstOrDefaultAsync<dynamic>(authorSql, new { UserId = authorUserId });

            _logger.LogInformation("Kind word posted by user {UserId} on bird {BirdId}", authorUserId, birdId);

            return new KindWordResultDto
            {
                Success = true,
                Message = "Your kind words have been shared",
                KindWord = new KindWordDto
                {
                    Id = kindWordId,
                    BirdId = birdId,
                    AuthorUserId = authorUserId,
                    AuthorName = author?.name ?? "You",
                    AuthorProfileImage = author?.profile_image,
                    Text = dto.Text.Trim(),
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        public async Task<KindWordResultDto> DeleteKindWordAsync(Guid kindWordId, Guid requestingUserId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get kind word and check ownership
            var kindWordSql = @"
                SELECT kw.id, kw.bird_id, kw.author_user_id, b.owner_id as bird_owner_id
                FROM kind_words kw
                JOIN birds b ON kw.bird_id = b.bird_id
                WHERE kw.id = @KindWordId AND kw.is_deleted = false";
            var kindWord = await connection.QueryFirstOrDefaultAsync<dynamic>(kindWordSql, new { KindWordId = kindWordId });

            if (kindWord == null)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Kind word not found"
                };
            }

            // Check permission: must be bird owner or the author
            bool isBirdOwner = kindWord.bird_owner_id == requestingUserId;
            bool isAuthor = kindWord.author_user_id == requestingUserId;

            if (!isBirdOwner && !isAuthor)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "You don't have permission to delete this kind word"
                };
            }

            // Soft delete (silent - no notification)
            var deleteSql = "UPDATE kind_words SET is_deleted = true WHERE id = @KindWordId";
            await connection.ExecuteAsync(deleteSql, new { KindWordId = kindWordId });

            _logger.LogInformation("Kind word {KindWordId} deleted by user {UserId}", kindWordId, requestingUserId);

            return new KindWordResultDto
            {
                Success = true,
                Message = "Kind word removed"
            };
        }

        public async Task<KindWordResultDto> SetKindWordsEnabledAsync(Guid birdId, Guid ownerUserId, bool enabled)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check ownership
            var birdSql = "SELECT bird_id, owner_id FROM birds WHERE bird_id = @BirdId";
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = birdId });

            if (bird == null)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Bird not found"
                };
            }

            if (bird.owner_id != ownerUserId)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Only the bird owner can change this setting"
                };
            }

            // Update setting
            var updateSql = "UPDATE birds SET kind_words_enabled = @Enabled WHERE bird_id = @BirdId";
            await connection.ExecuteAsync(updateSql, new { BirdId = birdId, Enabled = enabled });

            _logger.LogInformation("Kind words {Status} for bird {BirdId} by owner {UserId}",
                enabled ? "enabled" : "disabled", birdId, ownerUserId);

            return new KindWordResultDto
            {
                Success = true,
                Message = enabled ? "Kind words enabled" : "Kind words disabled"
            };
        }

        public async Task<KindWordResultDto> BlockUserAsync(Guid birdId, Guid ownerUserId, Guid userToBlockId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check ownership
            var birdSql = "SELECT bird_id, owner_id FROM birds WHERE bird_id = @BirdId";
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = birdId });

            if (bird == null)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Bird not found"
                };
            }

            if (bird.owner_id != ownerUserId)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Only the bird owner can block users"
                };
            }

            // Check if already blocked
            var existsSql = @"
                SELECT 1 FROM kind_words_blocked_users
                WHERE bird_id = @BirdId AND blocked_user_id = @BlockedUserId";
            var exists = await connection.QueryFirstOrDefaultAsync<int?>(existsSql, new
            {
                BirdId = birdId,
                BlockedUserId = userToBlockId
            });

            if (exists.HasValue)
            {
                return new KindWordResultDto
                {
                    Success = true,
                    Message = "User already blocked"
                };
            }

            // Insert block (silent - no notification)
            var insertSql = @"
                INSERT INTO kind_words_blocked_users (id, bird_id, blocked_user_id, blocked_at, blocked_by_user_id)
                VALUES (@Id, @BirdId, @BlockedUserId, @BlockedAt, @BlockedByUserId)";

            await connection.ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                BirdId = birdId,
                BlockedUserId = userToBlockId,
                BlockedAt = DateTime.UtcNow,
                BlockedByUserId = ownerUserId
            });

            _logger.LogInformation("User {BlockedUserId} blocked from bird {BirdId} by owner {OwnerId}",
                userToBlockId, birdId, ownerUserId);

            return new KindWordResultDto
            {
                Success = true,
                Message = "User blocked"
            };
        }

        public async Task<KindWordResultDto> UnblockUserAsync(Guid birdId, Guid ownerUserId, Guid userToUnblockId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check ownership
            var birdSql = "SELECT bird_id, owner_id FROM birds WHERE bird_id = @BirdId";
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = birdId });

            if (bird == null)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Bird not found"
                };
            }

            if (bird.owner_id != ownerUserId)
            {
                return new KindWordResultDto
                {
                    Success = false,
                    Message = "Only the bird owner can unblock users"
                };
            }

            // Delete block
            var deleteSql = @"
                DELETE FROM kind_words_blocked_users
                WHERE bird_id = @BirdId AND blocked_user_id = @BlockedUserId";
            var deleted = await connection.ExecuteAsync(deleteSql, new
            {
                BirdId = birdId,
                BlockedUserId = userToUnblockId
            });

            if (deleted == 0)
            {
                return new KindWordResultDto
                {
                    Success = true,
                    Message = "User was not blocked"
                };
            }

            _logger.LogInformation("User {UnblockedUserId} unblocked from bird {BirdId} by owner {OwnerId}",
                userToUnblockId, birdId, ownerUserId);

            return new KindWordResultDto
            {
                Success = true,
                Message = "User unblocked"
            };
        }

        public async Task<(bool CanPost, string? Reason)> CanUserPostAsync(Guid birdId, Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            return await CanUserPostInternalAsync(connection, birdId, userId);
        }

        private async Task<(bool CanPost, string? Reason)> CanUserPostInternalAsync(
            System.Data.IDbConnection connection, Guid birdId, Guid userId)
        {
            // Check if user is blocked
            var blockedSql = @"
                SELECT 1 FROM kind_words_blocked_users
                WHERE bird_id = @BirdId AND blocked_user_id = @UserId";
            var isBlocked = await connection.QueryFirstOrDefaultAsync<int?>(blockedSql, new
            {
                BirdId = birdId,
                UserId = userId
            });

            if (isBlocked.HasValue)
            {
                // Silent: don't reveal they're blocked
                return (false, "You cannot post kind words on this bird");
            }

            // Check if user has supported the bird (financial support)
            var supportedSql = @"
                SELECT 1 FROM support_transactions
                WHERE bird_id = @BirdId AND supporter_id = @UserId";
            var hasSupported = await connection.QueryFirstOrDefaultAsync<int?>(supportedSql, new
            {
                BirdId = birdId,
                UserId = userId
            });

            if (hasSupported.HasValue)
            {
                return (true, null);
            }

            // Check if user loves/follows the bird
            var lovesSql = @"
                SELECT 1 FROM loves
                WHERE bird_id = @BirdId AND user_id = @UserId";
            var lovesbird = await connection.QueryFirstOrDefaultAsync<int?>(lovesSql, new
            {
                BirdId = birdId,
                UserId = userId
            });

            if (lovesbird.HasValue)
            {
                return (true, null);
            }

            return (false, "Love or support this bird to leave a kind word");
        }

        private (bool IsValid, string? Error) ValidateText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, "Please write something kind");
            }

            var trimmed = text.Trim();

            if (trimmed.Length > MaxTextLength)
            {
                return (false, $"Please keep messages kind and under {MaxTextLength} characters");
            }

            // Check for URLs
            if (UrlRegex.IsMatch(trimmed))
            {
                return (false, "Please share kind words only. Links are not allowed.");
            }

            // Check for emoji-only (must have at least some non-emoji text)
            var textWithoutEmoji = Regex.Replace(trimmed, @"\p{Cs}|\p{So}", "").Trim();
            if (textWithoutEmoji.Length < 3)
            {
                return (false, "Please write a few words along with any emojis");
            }

            return (true, null);
        }
    }
}
