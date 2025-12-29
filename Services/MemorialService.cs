using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class MemorialService : IMemorialService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<MemorialService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IS3Service _s3Service;
        private readonly IContentModerationService _moderationService;
        private readonly IMemorialEmailService _memorialEmailService;

        public MemorialService(
            IDbConnectionFactory dbFactory,
            ILogger<MemorialService> logger,
            INotificationService notificationService,
            IS3Service s3Service,
            IContentModerationService moderationService,
            IMemorialEmailService memorialEmailService)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _notificationService = notificationService;
            _s3Service = s3Service;
            _moderationService = moderationService;
            _memorialEmailService = memorialEmailService;
        }

        public async Task<MarkMemorialResponseDto> MarkBirdAsMemorialAsync(Guid birdId, Guid ownerId, MemorialRequestDto request)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            try
            {
                // Verify bird exists and user is owner
                var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT bird_id, owner_id, name, species, is_memorial FROM birds WHERE bird_id = @BirdId",
                    new { BirdId = birdId });

                if (bird == null)
                {
                    return new MarkMemorialResponseDto
                    {
                        Success = false,
                        Message = "Bird not found"
                    };
                }

                if ((Guid)bird.owner_id != ownerId)
                {
                    return new MarkMemorialResponseDto
                    {
                        Success = false,
                        Message = "Only the bird owner can mark the bird as memorial"
                    };
                }

                if (bird.is_memorial)
                {
                    return new MarkMemorialResponseDto
                    {
                        Success = false,
                        Message = "Bird is already marked as memorial"
                    };
                }

                var memorialDate = request.MemorialDate ?? DateTime.UtcNow;

                // Update bird to memorial status
                await connection.ExecuteAsync(@"
                    UPDATE birds
                    SET is_memorial = true,
                        memorial_date = @MemorialDate,
                        memorial_reason = @MemorialReason
                    WHERE bird_id = @BirdId",
                    new
                    {
                        BirdId = birdId,
                        MemorialDate = memorialDate,
                        MemorialReason = request.MemorialReason
                    });

                _logger.LogInformation("Bird {BirdId} marked as memorial by owner {OwnerId}", birdId, ownerId);

                // Get supporters for notifications (run in background)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendMemorialNotificationsAsync(birdId, bird.name, ownerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send memorial notifications for bird {BirdId}", birdId);
                    }
                });

                // Get memorial details for response
                var memorialDetails = await GetMemorialDetailsAsync(birdId);

                return new MarkMemorialResponseDto
                {
                    Success = true,
                    Message = "Bird marked as memorial",
                    Bird = memorialDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking bird {BirdId} as memorial", birdId);
                return new MarkMemorialResponseDto
                {
                    Success = false,
                    Message = "An error occurred while marking the bird as memorial"
                };
            }
        }

        public async Task<MemorialBirdDto?> GetMemorialDetailsAsync(Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT
                    b.bird_id,
                    b.name,
                    b.species,
                    b.is_memorial,
                    b.memorial_date,
                    b.memorial_reason,
                    b.image_url,
                    COALESCE(b.loved_count, 0) as loved_by,
                    COALESCE(b.supported_count, 0) as supported_by,
                    COALESCE(b.donation_cents, 0) as donation_cents,
                    b.description as owner_message
                FROM birds b
                WHERE b.bird_id = @BirdId AND b.is_memorial = true",
                new { BirdId = birdId });

            if (bird == null) return null;

            // Get messages count
            var messagesCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM memorial_messages WHERE bird_id = @BirdId AND is_approved = true",
                new { BirdId = birdId });

            // Generate download URL for image
            string? imageUrl = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(bird.image_url))
                {
                    imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.image_url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate download URL for memorial bird {BirdId}", birdId);
            }

            return new MemorialBirdDto
            {
                BirdId = (Guid)bird.bird_id,
                Name = bird.name ?? string.Empty,
                Species = bird.species,
                IsMemorial = bird.is_memorial,
                MemorialDate = bird.memorial_date,
                MemorialReason = bird.memorial_reason,
                ImageUrl = imageUrl,
                CoverImageUrl = imageUrl, // Use same image for now
                Stats = new MemorialStatsDto
                {
                    LovedBy = bird.loved_by,
                    SupportedBy = bird.supported_by,
                    TotalSupportReceived = (bird.donation_cents ?? 0) / 100m
                },
                OwnerMessage = bird.owner_message,
                MessagesCount = messagesCount
            };
        }

        public async Task<MemorialMessageDto> AddMemorialMessageAsync(Guid birdId, Guid userId, CreateMemorialMessageDto message)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Verify bird is memorial
            var isMemorial = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId AND is_memorial = true)",
                new { BirdId = birdId });

            if (!isMemorial)
            {
                throw new InvalidOperationException("Bird is not marked as memorial");
            }

            // Content moderation check
            var moderationResult = await _moderationService.ModerateMemorialMessageAsync(message.Message);
            if (moderationResult.IsBlocked)
            {
                _logger.LogWarning("Memorial message blocked by moderation for user {UserId}, bird {BirdId}. Reason: {Reason}",
                    userId, birdId, moderationResult.BlockReason);
                throw new InvalidOperationException($"Message blocked by moderation: {moderationResult.BlockReason}");
            }

            // Check rate limit
            var canPost = await CheckMessageRateLimitAsync(userId, birdId);
            if (!canPost)
            {
                throw new InvalidOperationException("Rate limit exceeded. Maximum 3 messages per bird per day.");
            }

            var messageId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Insert message
            await connection.ExecuteAsync(@"
                INSERT INTO memorial_messages (message_id, bird_id, user_id, message, is_approved, created_at, updated_at)
                VALUES (@MessageId, @BirdId, @UserId, @Message, true, @Now, @Now)",
                new
                {
                    MessageId = messageId,
                    BirdId = birdId,
                    UserId = userId,
                    Message = message.Message,
                    Now = now
                });

            // Get user info for response
            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT user_id, name FROM users WHERE user_id = @UserId",
                new { UserId = userId });

            _logger.LogInformation("Memorial message added for bird {BirdId} by user {UserId}", birdId, userId);

            return new MemorialMessageDto
            {
                MessageId = messageId,
                BirdId = birdId,
                UserId = userId,
                UserName = user?.name ?? "Anonymous",
                Message = message.Message,
                CreatedAt = now
            };
        }

        public async Task<MemorialMessagePageDto> GetMemorialMessagesAsync(Guid birdId, int page = 1, int pageSize = 20, string sortBy = "recent")
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            pageSize = Math.Min(pageSize, 50); // Max 50 per page
            var offset = (page - 1) * pageSize;

            var orderBy = sortBy == "popular" ? "mm.created_at ASC" : "mm.created_at DESC"; // Placeholder for popularity

            var sql = $@"
                SELECT 
                    mm.message_id,
                    mm.bird_id,
                    mm.user_id,
                    mm.message,
                    mm.created_at,
                    u.name as user_name
                FROM memorial_messages mm
                LEFT JOIN users u ON mm.user_id = u.user_id
                WHERE mm.bird_id = @BirdId AND mm.is_approved = true
                ORDER BY {orderBy}
                LIMIT @PageSize OFFSET @Offset";

            var messages = await connection.QueryAsync<dynamic>(sql, new
            {
                BirdId = birdId,
                PageSize = pageSize,
                Offset = offset
            });

            var totalCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM memorial_messages WHERE bird_id = @BirdId AND is_approved = true",
                new { BirdId = birdId });

            var messageDtos = messages.Select(m => new MemorialMessageDto
            {
                MessageId = (Guid)m.message_id,
                BirdId = (Guid)m.bird_id,
                UserId = (Guid)m.user_id,
                UserName = m.user_name ?? "Anonymous",
                Message = m.message ?? string.Empty,
                CreatedAt = m.created_at
            }).ToList();

            return new MemorialMessagePageDto
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Messages = messageDtos
            };
        }

        public async Task<bool> DeleteMemorialMessageAsync(Guid messageId, Guid requesterId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get message details including bird owner
            var messageInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT 
                    mm.user_id as author_id,
                    b.owner_id as bird_owner_id
                FROM memorial_messages mm
                INNER JOIN birds b ON mm.bird_id = b.bird_id
                WHERE mm.message_id = @MessageId",
                new { MessageId = messageId });

            if (messageInfo == null) return false;

            // Only message author or bird owner can delete
            if ((Guid)messageInfo.author_id != requesterId && (Guid)messageInfo.bird_owner_id != requesterId)
            {
                return false;
            }

            var deleted = await connection.ExecuteAsync(
                "DELETE FROM memorial_messages WHERE message_id = @MessageId",
                new { MessageId = messageId });

            _logger.LogInformation("Memorial message {MessageId} deleted by user {RequesterId}", messageId, requesterId);

            return deleted > 0;
        }

        public async Task<bool> IsMemorialBirdAsync(Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId AND is_memorial = true)",
                new { BirdId = birdId });
        }

        public async Task<bool> CheckMessageRateLimitAsync(Guid userId, Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var messageCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) 
                FROM memorial_messages 
                WHERE user_id = @UserId 
                AND bird_id = @BirdId 
                AND created_at > @OneDayAgo",
                new { UserId = userId, BirdId = birdId, OneDayAgo = oneDayAgo });

            return messageCount < 3;
        }

        private async Task SendMemorialNotificationsAsync(Guid birdId, string birdName, Guid ownerId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get bird details for email
            var bird = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT bird_id, name, image_url, memorial_date, memorial_reason
                FROM birds WHERE bird_id = @BirdId",
                new { BirdId = birdId });

            string? birdImageUrl = null;
            if (bird != null && !string.IsNullOrWhiteSpace(bird.image_url as string))
            {
                try
                {
                    birdImageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.image_url);
                }
                catch { }
            }

            // Get all supporters with their details (users who donated or loved the bird)
            var supporters = await connection.QueryAsync<dynamic>(@"
                SELECT DISTINCT u.user_id, u.email, u.name, u.language_code,
                    COALESCE(st.total_supported, 0) as total_supported
                FROM (
                    SELECT supporter_id as user_id FROM support_transactions WHERE bird_id = @BirdId
                    UNION
                    SELECT user_id FROM loves WHERE bird_id = @BirdId
                ) AS s
                JOIN users u ON s.user_id = u.user_id
                LEFT JOIN (
                    SELECT supporter_id, SUM(amount) as total_supported
                    FROM support_transactions
                    WHERE bird_id = @BirdId
                    GROUP BY supporter_id
                ) st ON st.supporter_id = u.user_id
                WHERE u.user_id != @OwnerId",
                new { BirdId = birdId, OwnerId = ownerId });

            // Send notification and email to each supporter
            foreach (var supporter in supporters)
            {
                var supporterId = (Guid)supporter.user_id;
                var supporterEmail = supporter.email as string;
                var supporterName = supporter.name as string ?? "Supporter";
                var languageCode = supporter.language_code as string;
                var totalSupported = (decimal)(supporter.total_supported ?? 0m);

                try
                {
                    // In-app notification
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = supporterId,
                        Type = Models.Enums.NotificationType.BirdMemorial,
                        Title = $"In Loving Memory: {birdName}",
                        Message = $"We're writing to let you know that {birdName}, a bird you supported, has passed away. You can visit their memorial page to share memories.",
                        Priority = Models.Enums.NotificationPriority.High,
                        Channels = Models.Enums.NotificationChannel.InApp,
                        DeepLink = $"/birds/{birdId}/memorial",
                        BirdId = birdId
                    });

                    // Send gentle memorial email
                    if (!string.IsNullOrWhiteSpace(supporterEmail))
                    {
                        await _memorialEmailService.SendMemorialNotificationAsync(new MemorialNotificationDto
                        {
                            SupporterEmail = supporterEmail,
                            SupporterName = supporterName,
                            BirdName = birdName,
                            BirdImageUrl = birdImageUrl,
                            MemorialDate = bird?.memorial_date as DateTime?,
                            MemorialReason = bird?.memorial_reason as string,
                            BirdId = birdId,
                            TotalSupportGiven = totalSupported,
                            Language = languageCode
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send memorial notification to supporter {SupporterId}", supporterId);
                }
            }

            _logger.LogInformation("Memorial notifications and emails sent for bird {BirdId} to {Count} supporters",
                birdId, supporters.Count());
        }
    }
}
