using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Models.Payout;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class MemorialService : IMemorialService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<MemorialService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IS3Service _s3Service;

        public MemorialService(
            IDbConnectionFactory dbFactory,
            ILogger<MemorialService> logger,
            INotificationService notificationService,
            IS3Service s3Service)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _notificationService = notificationService;
            _s3Service = s3Service;
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

                // Validate charity name if choice is charity
                if (request.FundsRedirectionChoice == "charity" && string.IsNullOrWhiteSpace(request.CharityName))
                {
                    return new MarkMemorialResponseDto
                    {
                        Success = false,
                        Message = "Charity name is required when redirecting funds to charity"
                    };
                }

                var memorialDate = request.MemorialDate ?? DateTime.UtcNow;

                // Update bird to memorial status
                await connection.ExecuteAsync(@"
                    UPDATE birds 
                    SET is_memorial = true,
                        memorial_date = @MemorialDate,
                        memorial_reason = @MemorialReason,
                        funds_redirection_choice = @FundsRedirectionChoice
                    WHERE bird_id = @BirdId",
                    new
                    {
                        BirdId = birdId,
                        MemorialDate = memorialDate,
                        MemorialReason = request.MemorialReason,
                        FundsRedirectionChoice = request.FundsRedirectionChoice
                    });

                // Calculate remaining balance from payout system
                var remainingBalance = await connection.QueryFirstOrDefaultAsync<decimal?>(
                    "SELECT available_balance FROM payout_balances WHERE user_id = @OwnerId",
                    new { OwnerId = ownerId }) ?? 0;

                // Create fund redirection record
                var redirectionId = Guid.NewGuid();
                await connection.ExecuteAsync(@"
                    INSERT INTO memorial_fund_redirections 
                    (redirection_id, bird_id, previous_owner_id, remaining_balance, redirection_type, charity_name, status, created_at, updated_at)
                    VALUES 
                    (@RedirectionId, @BirdId, @OwnerId, @RemainingBalance, @RedirectionType, @CharityName, 'pending', @Now, @Now)",
                    new
                    {
                        RedirectionId = redirectionId,
                        BirdId = birdId,
                        OwnerId = ownerId,
                        RemainingBalance = remainingBalance,
                        RedirectionType = request.FundsRedirectionChoice,
                        CharityName = request.CharityName,
                        Now = DateTime.UtcNow
                    });

                _logger.LogInformation("Bird {BirdId} marked as memorial by owner {OwnerId}. Fund redirection: {RedirectionType}",
                    birdId, ownerId, request.FundsRedirectionChoice);

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
                    b.funds_redirection_choice,
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

            // Get remaining balance if exists
            var remainingBalance = await connection.QueryFirstOrDefaultAsync<decimal?>(
                "SELECT remaining_balance FROM memorial_fund_redirections WHERE bird_id = @BirdId ORDER BY created_at DESC LIMIT 1",
                new { BirdId = birdId }) ?? 0;

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
                MessagesCount = messagesCount,
                FundsRedirectionChoice = bird.funds_redirection_choice,
                RemainingBalance = remainingBalance
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

        public async Task<MemorialFundRedirectionDto> ProcessFundRedirectionAsync(Guid birdId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get redirection record
            var redirection = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT 
                    mfr.*,
                    b.name as bird_name
                FROM memorial_fund_redirections mfr
                INNER JOIN birds b ON mfr.bird_id = b.bird_id
                WHERE mfr.bird_id = @BirdId AND mfr.status = 'pending'
                ORDER BY mfr.created_at DESC
                LIMIT 1",
                new { BirdId = birdId });

            if (redirection == null)
            {
                throw new InvalidOperationException("No pending fund redirection found for this bird");
            }

            var redirectionId = (Guid)redirection.redirection_id;
            var redirectionType = redirection.redirection_type;
            var remainingBalance = (decimal)redirection.remaining_balance;
            var ownerId = (Guid)redirection.previous_owner_id;

            // Update status to processing
            await connection.ExecuteAsync(
                "UPDATE memorial_fund_redirections SET status = 'processing', updated_at = @Now WHERE redirection_id = @RedirectionId",
                new { RedirectionId = redirectionId, Now = DateTime.UtcNow });

            try
            {
                string notes = string.Empty;
                string? transactionId = null;

                switch (redirectionType)
                {
                    case "emergency_fund":
                        // Transfer to platform emergency fund
                        // This would integrate with your payment system
                        notes = $"Transferred {remainingBalance:C} to platform emergency fund";
                        transactionId = $"EMERG-{Guid.NewGuid().ToString().Substring(0, 8)}";
                        _logger.LogInformation("Memorial fund {Amount} redirected to emergency fund for bird {BirdId}", remainingBalance, birdId);
                        break;

                    case "owner_keeps":
                        // Payout to owner (integrate with existing payout system)
                        notes = $"Scheduled payout of {remainingBalance:C} to owner";
                        transactionId = $"PAYOUT-{Guid.NewGuid().ToString().Substring(0, 8)}";
                        _logger.LogInformation("Memorial fund {Amount} scheduled for payout to owner {OwnerId}", remainingBalance, ownerId);
                        break;

                    case "charity":
                        // Donate to charity
                        var charityName = redirection.charity_name as string ?? "Bird Conservation Charity";
                        notes = $"Donated {remainingBalance:C} to {charityName}";
                        transactionId = $"CHARITY-{Guid.NewGuid().ToString().Substring(0, 8)}";
                        _logger.LogInformation("Memorial fund {Amount} donated to charity {CharityName} for bird {BirdId}",
                            remainingBalance, charityName, birdId);
                        break;
                }

                // Update redirection to completed
                await connection.ExecuteAsync(@"
                    UPDATE memorial_fund_redirections 
                    SET status = 'completed',
                        processed_at = @Now,
                        transaction_id = @TransactionId,
                        notes = @Notes,
                        updated_at = @Now
                    WHERE redirection_id = @RedirectionId",
                    new
                    {
                        RedirectionId = redirectionId,
                        Now = DateTime.UtcNow,
                        TransactionId = transactionId,
                        Notes = notes
                    });

                return new MemorialFundRedirectionDto
                {
                    RedirectionId = redirectionId,
                    BirdId = birdId,
                    BirdName = redirection.bird_name,
                    RemainingBalance = remainingBalance,
                    RedirectionType = redirectionType,
                    CharityName = redirection.charity_name,
                    Status = "completed",
                    ProcessedAt = DateTime.UtcNow,
                    TransactionId = transactionId,
                    Notes = notes,
                    CreatedAt = redirection.created_at
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fund redirection for bird {BirdId}", birdId);
                
                // Update to failed status
                await connection.ExecuteAsync(@"
                    UPDATE memorial_fund_redirections 
                    SET status = 'failed',
                        notes = @Notes,
                        updated_at = @Now
                    WHERE redirection_id = @RedirectionId",
                    new
                    {
                        RedirectionId = redirectionId,
                        Notes = $"Failed: {ex.Message}",
                        Now = DateTime.UtcNow
                    });

                throw;
            }
        }

        private async Task SendMemorialNotificationsAsync(Guid birdId, string birdName, Guid ownerId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get all supporters (users who donated or loved the bird)
            var supporters = await connection.QueryAsync<Guid>(@"
                SELECT DISTINCT user_id FROM (
                    SELECT supporter_id as user_id FROM support_transactions WHERE bird_id = @BirdId
                    UNION
                    SELECT user_id FROM loves WHERE bird_id = @BirdId
                ) AS supporters
                WHERE user_id != @OwnerId",
                new { BirdId = birdId, OwnerId = ownerId });

            // Send notification to each supporter
            foreach (var supporterId in supporters)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = supporterId,
                        Type = Models.Enums.NotificationType.BirdMemorial,
                        Title = $"??? Remembering {birdName}",
                        Message = $"We're writing to let you know that {birdName}, a bird you supported, has passed away. You can visit their memorial page to share memories.",
                        Priority = Models.Enums.NotificationPriority.High,
                        Channels = Models.Enums.NotificationChannel.InApp | Models.Enums.NotificationChannel.Email,
                        DeepLink = $"/birds/{birdId}/memorial",
                        BirdId = birdId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send memorial notification to supporter {SupporterId}", supporterId);
                }
            }

            _logger.LogInformation("Memorial notifications sent for bird {BirdId} to {Count} supporters",
                birdId, supporters.Count());
        }
    }
}
