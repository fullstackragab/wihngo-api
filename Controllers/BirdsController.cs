namespace Wihngo.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using Dapper;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;
    using System.Text.Json;

    [Route("api/[controller]")]
    [ApiController]
    public class BirdsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IS3Service _s3Service;
        private readonly ILogger<BirdsController> _logger;

        public BirdsController(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IMapper mapper, 
            INotificationService notificationService,
            IS3Service s3Service,
            ILogger<BirdsController> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _notificationService = notificationService;
            _s3Service = s3Service;
            _logger = logger;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        private async Task<bool> EnsureOwner(Guid birdId)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return false;
            var bird = await _db.Birds.FindAsync(birdId);
            if (bird == null) return false;
            return bird.OwnerId == userId.Value;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BirdSummaryDto>>> Get()
        {
            var userId = GetUserIdClaim();
            
            // Get all bird IDs this user has loved (if authenticated)
            HashSet<Guid> lovedBirdIds = new HashSet<Guid>();
            if (userId.HasValue)
            {
                var sql = "SELECT bird_id FROM loves WHERE user_id = @UserId";
                var lovedIds = await _dbFactory.QueryListAsync<Guid>(sql, new { UserId = userId.Value });
                lovedBirdIds = new HashSet<Guid>(lovedIds);
            }

            // Get all birds with necessary fields
            var birdsSql = @"
                SELECT 
                    bird_id as BirdId,
                    name as Name,
                    species as Species,
                    image_url as ImageUrl,
                    video_url as VideoUrl,
                    tagline as Tagline,
                    loved_count as LovedCount,
                    supported_count as SupportedCount,
                    owner_id as OwnerId
                FROM birds";

            var birds = await _dbFactory.QueryListAsync<dynamic>(birdsSql);

            // Generate download URLs and map to DTOs
            var birdDtos = new List<BirdSummaryDto>();
            foreach (var bird in birds)
            {
                string? imageUrl = null;
                string? videoUrl = null;

                try
                {
                    if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
                    {
                        imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageUrl);
                    }
                    if (!string.IsNullOrWhiteSpace(bird.VideoUrl))
                    {
                        videoUrl = await _s3Service.GenerateDownloadUrlAsync(bird.VideoUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URLs for bird {BirdId}", (Guid)bird.BirdId);
                }

                birdDtos.Add(new BirdSummaryDto
                {
                    BirdId = bird.BirdId,
                    Name = bird.Name,
                    Species = bird.Species,
                    ImageS3Key = bird.ImageUrl,
                    ImageUrl = imageUrl,
                    VideoS3Key = bird.VideoUrl,
                    VideoUrl = videoUrl,
                    Tagline = bird.Tagline,
                    LovedBy = bird.LovedCount,
                    SupportedBy = bird.SupportedCount,
                    OwnerId = bird.OwnerId,
                    IsLoved = lovedBirdIds.Contains((Guid)bird.BirdId)
                });
            }

            return Ok(birdDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BirdProfileDto>> Get(Guid id)
        {
            var bird = await _db.Birds
                .AsNoTracking()
                .Include(b => b.Owner)
                .Include(b => b.Stories)
                .Include(b => b.SupportTransactions)
                .FirstOrDefaultAsync(b => b.BirdId == id);

            if (bird == null) return NotFound();

            var dto = _mapper.Map<BirdProfileDto>(bird);

            // Fill counts explicitly
            dto.LovedBy = bird.LovedCount;
            dto.SupportedBy = bird.SupportTransactions?.Count ?? 0;
            
            // Store S3 keys
            dto.ImageS3Key = bird.ImageUrl;
            dto.VideoS3Key = bird.VideoUrl;

            // Generate download URLs
            try
            {
                if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
                {
                    dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageUrl);
                }
                if (!string.IsNullOrWhiteSpace(bird.VideoUrl))
                {
                    dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(bird.VideoUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate download URLs for bird {BirdId}", id);
            }

            // Check if current user has loved this bird
            var userId = GetUserIdClaim();
            if (userId.HasValue)
            {
                var loveSql = "SELECT EXISTS(SELECT 1 FROM loves WHERE user_id = @UserId AND bird_id = @BirdId)";
                dto.IsLoved = await _dbFactory.ExecuteScalarAsync<bool>(loveSql, new { UserId = userId.Value, BirdId = id });
            }
            else
            {
                dto.IsLoved = false;
            }

            // Map owner summary
            if (bird.Owner != null)
            {
                dto.Owner = new UserSummaryDto { UserId = bird.Owner.UserId, Name = bird.Owner.Name };
            }

            // Populate personality/fun facts/conservation if separate tables exist
            dto.Personality = dto.Personality ?? new List<string>
            {
                "Fearless and territorial",
                "Incredibly vocal for their size",
                "Early risers who sing before dawn",
                "Devoted parents"
            };

            dto.FunFacts = dto.FunFacts ?? new List<string>
            {
                "Males perform spectacular dive displays, reaching speeds of 60 mph",
                "They can remember every flower they've visited",
                "Their heart beats up to 1,260 times per minute",
                "They're one of the few hummingbirds that sing"
            };

            dto.Conservation ??= new ConservationDto
            {
                Status = "Least Concern",
                Needs = "Native plant gardens, year-round nectar sources, pesticide-free habitats"
            };

            return Ok(dto);
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<BirdProfileDto>> Profile(Guid id)
        {
            // Reuse existing profile logic
            return await Get(id);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BirdSummaryDto>> Post([FromBody] BirdCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Validate S3 keys
            if (string.IsNullOrWhiteSpace(dto.ImageS3Key) || string.IsNullOrWhiteSpace(dto.VideoS3Key))
            {
                return BadRequest(new { message = "ImageS3Key and VideoS3Key are required" });
            }

            // Verify files exist in S3
            var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
            var videoExists = await _s3Service.FileExistsAsync(dto.VideoS3Key);

            if (!imageExists)
            {
                return BadRequest(new { message = "Bird profile image not found in S3. Please upload the file first." });
            }

            if (!videoExists)
            {
                return BadRequest(new { message = "Bird video not found in S3. Please upload the file first." });
            }

            var bird = new Bird
            {
                BirdId = Guid.NewGuid(),
                OwnerId = userId.Value,
                Name = dto.Name,
                Species = dto.Species,
                Tagline = dto.Tagline,
                Description = dto.Description,
                ImageUrl = dto.ImageS3Key,
                VideoUrl = dto.VideoS3Key,
                CreatedAt = DateTime.UtcNow
            };

            _db.Birds.Add(bird);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Bird created: {BirdId} by user {UserId}", bird.BirdId, userId.Value);

            // Generate download URLs for response
            string? imageUrl = null;
            string? videoUrl = null;
            try
            {
                imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageUrl);
                videoUrl = await _s3Service.GenerateDownloadUrlAsync(bird.VideoUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate download URLs for bird {BirdId}", bird.BirdId);
            }

            var summary = new BirdSummaryDto
            {
                BirdId = bird.BirdId,
                Name = bird.Name,
                Species = bird.Species,
                ImageS3Key = bird.ImageUrl,
                ImageUrl = imageUrl,
                VideoS3Key = bird.VideoUrl,
                VideoUrl = videoUrl,
                Tagline = bird.Tagline,
                LovedBy = 0,
                SupportedBy = 0,
                OwnerId = bird.OwnerId,
                IsLoved = false
            };

            return CreatedAtAction(nameof(Get), new { id = bird.BirdId }, summary);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(Guid id, [FromBody] BirdCreateDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            bird.Name = dto.Name;
            bird.Species = dto.Species;
            bird.Description = dto.Description;
            bird.Tagline = dto.Tagline;

            // Update image if provided and different
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key) && dto.ImageS3Key != bird.ImageUrl)
            {
                var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                if (!imageExists)
                {
                    return BadRequest(new { message = "Bird profile image not found in S3." });
                }

                // Delete old image
                if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
                {
                    try
                    {
                        await _s3Service.DeleteFileAsync(bird.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old bird image");
                    }
                }

                bird.ImageUrl = dto.ImageS3Key;
            }

            // Update video if provided and different
            if (!string.IsNullOrWhiteSpace(dto.VideoS3Key) && dto.VideoS3Key != bird.VideoUrl)
            {
                var videoExists = await _s3Service.FileExistsAsync(dto.VideoS3Key);
                if (!videoExists)
                {
                    return BadRequest(new { message = "Bird video not found in S3." });
                }

                // Delete old video
                if (!string.IsNullOrWhiteSpace(bird.VideoUrl))
                {
                    try
                    {
                        await _s3Service.DeleteFileAsync(bird.VideoUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old bird video");
                    }
                }

                bird.VideoUrl = dto.VideoS3Key;
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Bird updated: {BirdId}", id);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            // Delete media files from S3
            if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(bird.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete bird image from S3");
                }
            }

            if (!string.IsNullOrWhiteSpace(bird.VideoUrl))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(bird.VideoUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete bird video from S3");
                }
            }

            _db.Birds.Remove(bird);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Bird deleted: {BirdId}", id);
            
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/love")]
        public async Task<IActionResult> Love(Guid id)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized("User not found");

            var exists = await _db.Loves.FindAsync(userId, id);
            if (exists != null) return BadRequest("Already loved");

            var love = new Love { UserId = userId, BirdId = id };
            _db.Loves.Add(love);

            // Atomic increment
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = loved_count + 1 WHERE bird_id = {id}");
            await _db.SaveChangesAsync();

            // Send notification to bird owner
            if (bird.OwnerId != userId) // Don't notify if user loves their own bird
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var updatedBird = await _db.Birds.AsNoTracking().FirstOrDefaultAsync(b => b.BirdId == id);
                        var loveCount = updatedBird?.LovedCount ?? 0;

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = bird.OwnerId,
                            Type = NotificationType.BirdLoved,
                            Title = "Heart " + user.Name + " loved " + bird.Name + "!",
                            Message = $"{user.Name} loved your {bird.Species ?? "bird"}. You now have {loveCount} loves!",
                            Priority = NotificationPriority.Medium,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push,
                            DeepLink = $"/birds/{id}",
                            BirdId = id,
                            ActorUserId = userId
                        });

                        // Check for milestone
                        if (loveCount > 0 && IsMilestone(loveCount))
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = bird.OwnerId,
                                Type = NotificationType.MilestoneAchieved,
                                Title = "Celebration " + bird.Name + " reached " + loveCount + " loves!",
                                Message = $"Congratulations! Your bird is loved by {loveCount} people.",
                                Priority = NotificationPriority.High,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/birds/{id}",
                                BirdId = id
                            });
                        }
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/unlove")]
        public async Task<IActionResult> Unlove(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var love = await _db.Loves.FindAsync(userId, id);
            if (love == null) return NotFound();

            // Atomic decrement but not below zero
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = {id}");
            _db.Loves.Remove(love);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}/love")]
        public async Task<IActionResult> UnloveDelete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var love = await _db.Loves.FindAsync(userId, id);
            if (love == null) return NotFound("Love record not found");

            // Verify bird exists
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            // Atomic decrement but not below zero
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = {id}");
            _db.Loves.Remove(love);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id}/lovers")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> Lovers(Guid id)
        {
            var sql = @"
                SELECT 
                    l.user_id as UserId,
                    COALESCE(u.name, '') as Name
                FROM loves l
                LEFT JOIN users u ON l.user_id = u.user_id
                WHERE l.bird_id = @BirdId";

            var lovers = await _dbFactory.QueryListAsync<UserSummaryDto>(sql, new { BirdId = id });
            return Ok(lovers);
        }

        [Authorize]
        [HttpPost("{id}/support-usage")]
        public async Task<ActionResult<SupportUsageDto>> ReportSupportUsage(Guid id, [FromBody] SupportUsageDto dto)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var callerId)) return Unauthorized();

            // Only owner can report support usage
            if (bird.OwnerId != callerId) return Forbid();

            var usage = new SupportUsage
            {
                UsageId = Guid.NewGuid(),
                BirdId = id,
                ReportedBy = callerId,
                Amount = dto.Amount,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            // Use raw SQL INSERT instead of _db.Add
            var sql = @"
                INSERT INTO support_usages (usage_id, bird_id, reported_by, amount, description, created_at)
                VALUES (@UsageId, @BirdId, @ReportedBy, @Amount, @Description, @CreatedAt)";

            await _dbFactory.ExecuteAsync(sql, new
            {
                UsageId = usage.UsageId,
                BirdId = usage.BirdId,
                ReportedBy = usage.ReportedBy,
                Amount = usage.Amount,
                Description = usage.Description,
                CreatedAt = usage.CreatedAt
            });

            dto.UsageId = usage.UsageId;
            dto.BirdId = usage.BirdId;
            dto.ReportedBy = usage.ReportedBy;
            dto.CreatedAt = usage.CreatedAt;

            return CreatedAtAction(nameof(ReportSupportUsage), new { id = id, usageId = usage.UsageId }, dto);
        }

        [Authorize]
        [HttpPost("{id}/premium/subscribe")]
        public async Task<IActionResult> Subscribe(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var existing = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (existing != null) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;

            var subscription = new BirdPremiumSubscription
            {
                BirdId = id,
                OwnerId = userId,
                Status = "active",
                Plan = "monthly",
                Provider = "local",
                ProviderSubscriptionId = Guid.NewGuid().ToString(),
                PriceCents = 300,
                DurationDays = 30,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.BirdPremiumSubscriptions.Add(subscription);

            var bird = await _db.Birds.FindAsync(id);
            if (bird != null)
            {
                bird.IsPremium = true;
                bird.PremiumPlan = subscription.Plan;
                bird.PremiumExpiresAt = subscription.CurrentPeriodEnd;
                bird.MaxMediaCount = 20;
            }

            await _db.SaveChangesAsync();
            return Ok(new { subscriptionId = subscription.Id, expiry = subscription.CurrentPeriodEnd });
        }

        [Authorize]
        [HttpPost("{id}/premium/subscribe/lifetime")]
        public async Task<IActionResult> PurchaseLifetime(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var existing = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (existing != null) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;

            var subscription = new BirdPremiumSubscription
            {
                BirdId = id,
                OwnerId = userId,
                Status = "active",
                Plan = "lifetime",
                Provider = "local",
                ProviderSubscriptionId = Guid.NewGuid().ToString(),
                PriceCents = 7000,
                DurationDays = int.MaxValue,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.MaxValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.BirdPremiumSubscriptions.Add(subscription);

            var bird = await _db.Birds.FindAsync(id);
            if (bird != null)
            {
                bird.IsPremium = true;
                bird.PremiumPlan = subscription.Plan;
                bird.PremiumExpiresAt = null;
                bird.MaxMediaCount = 50;
            }

            await _db.SaveChangesAsync();
            return Ok(new { subscriptionId = subscription.Id, plan = subscription.Plan });
        }

        [Authorize]
        [HttpPatch("{id}/premium/style")]
        public async Task<IActionResult> UpdateStyle(Guid id, [FromBody] PremiumStyleDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            // Ensure active subscription
            var active = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (active == null) return Forbid("No active subscription");

            var json = JsonSerializer.Serialize(dto);
            bird.PremiumStyleJson = json;

            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPatch("{id}/premium/qr")]
        public async Task<IActionResult> UpdateQr(Guid id, [FromBody] string qrUrl)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            // Allow qr to be set only if premium
            if (!bird.IsPremium) return Forbid("Only premium birds can have QR codes");

            bird.QrCodeUrl = qrUrl;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/donate")]
        public async Task<IActionResult> DonateToBird(Guid id, [FromBody] long cents)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            // Record a simple support transaction (not handling external payments here)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var supporterId)) return Unauthorized();

            var tx = new SupportTransaction
            {
                TransactionId = Guid.NewGuid(),
                BirdId = id,
                SupporterId = supporterId,
                Amount = cents / 100m,
                Message = null,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTransactions.Add(tx);
            bird.DonationCents += cents;
            
            // Use raw SQL to get count
            var countSql = "SELECT COUNT(*) FROM support_transactions WHERE bird_id = @BirdId";
            bird.SupportedCount = await _dbFactory.ExecuteScalarAsync<int>(countSql, new { BirdId = id });

            await _db.SaveChangesAsync();

            // Send notification to bird owner
            if (bird.OwnerId != supporterId)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var supporter = await _db.Users.FindAsync(supporterId);
                        var amountDisplay = (cents / 100m).ToString("F2");

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = bird.OwnerId,
                            Type = NotificationType.BirdSupported,
                            Title = "Corn " + (supporter?.Name ?? "Someone") + " supported " + bird.Name + "!",
                            Message = $"{supporter?.Name ?? "Someone"} contributed ${amountDisplay} to support {bird.Name}. Total: ${bird.DonationCents / 100m:F2}",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = $"/birds/{id}",
                            BirdId = id,
                            TransactionId = tx.TransactionId,
                            ActorUserId = supporterId
                        });

                        // Also notify supporter
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = supporterId,
                            Type = NotificationType.PaymentReceived,
                            Title = "Checkmark Payment confirmed",
                            Message = $"Your ${amountDisplay} support for {bird.Name} was processed!",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = $"/support/{tx.TransactionId}",
                            BirdId = id,
                            TransactionId = tx.TransactionId
                        });
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            return Ok(new { totalDonated = bird.DonationCents });
        }

        private bool IsMilestone(int count)
        {
            int[] milestones = { 10, 50, 100, 500, 1000, 5000 };
            return milestones.Contains(count);
        }
    }
}
