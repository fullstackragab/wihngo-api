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
        private readonly IMemorialService _memorialService;

        public BirdsController(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IMapper mapper, 
            INotificationService notificationService,
            IS3Service s3Service,
            ILogger<BirdsController> logger,
            IMemorialService memorialService)
        {
            _db = db;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _notificationService = notificationService;
            _s3Service = s3Service;
            _logger = logger;
            _memorialService = memorialService;
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

        private bool IsMilestone(int count)
        {
            int[] milestones = { 10, 50, 100, 500, 1000, 5000 };
            return milestones.Contains(count);
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

            // Get all birds - PostgreSQL returns lowercase column names
            var birdsSql = @"
                SELECT 
                    bird_id,
                    name,
                    species,
                    image_url,
                    tagline,
                    COALESCE(loved_count, 0) loved_count,
                    COALESCE(supported_count, 0) supported_count,
                    owner_id
                FROM birds
                WHERE owner_id IS NOT NULL AND bird_id IS NOT NULL";

            var birds = await _dbFactory.QueryListAsync<dynamic>(birdsSql);

            // Generate download URLs and map to DTOs
            var birdDtos = new List<BirdSummaryDto>();
            foreach (var bird in birds)
            {
                // Skip records with null IDs (defensive check)
                if (bird.bird_id == null || bird.owner_id == null)
                {
                    _logger.LogWarning("Skipping bird record with NULL ID fields - data integrity issue");
                    continue;
                }

                string? imageUrl = null;

                // Access dynamic properties in lowercase (PostgreSQL default)
                Guid birdId = (Guid)bird.bird_id;
                Guid ownerId = (Guid)bird.owner_id;

                try
                {
                    if (!string.IsNullOrWhiteSpace(bird.image_url))
                    {
                        imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.image_url);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for bird {BirdId}", birdId);
                }

                birdDtos.Add(new BirdSummaryDto
                {
                    BirdId = birdId,
                    Name = bird.name ?? string.Empty,
                    Species = bird.species,
                    ImageS3Key = bird.image_url,
                    ImageUrl = imageUrl,
                    Tagline = bird.tagline,
                    LovedBy = bird.loved_count ?? 0,
                    SupportedBy = bird.supported_count ?? 0,
                    OwnerId = ownerId,
                    IsLoved = lovedBirdIds.Contains(birdId)
                });
            }

            return Ok(birdDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BirdProfileDto>> Get(Guid id)
        {
            // Get bird with owner using raw SQL  
            var birdSql = @"
                SELECT 
                    b.bird_id,
                    b.name,
                    b.species,
                    b.description,
                    b.image_url,
                    b.tagline,
                    COALESCE(b.loved_count, 0) loved_count,
                    COALESCE(b.supported_count, 0) supported_count,
                    b.owner_id,
                    b.created_at,
                    u.user_id owner_user_id,
                    u.name owner_name
                FROM birds b
                LEFT JOIN users u ON b.owner_id = u.user_id
                WHERE b.bird_id = @BirdId";

            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            var birdData = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = id });

            if (birdData == null) return NotFound();

            // Get stories for this bird
            var storiesSql = @"
                SELECT 
                    story_id,
                    bird_id,
                    content,
                    image_url,
                    created_at
                FROM stories
                WHERE bird_id = @BirdId
                ORDER BY created_at DESC";

            var stories = await connection.QueryAsync<Story>(storiesSql, new { BirdId = id });

            // Map to DTO (using property names from BirdProfileDto)
            var dto = new BirdProfileDto
            {
                CommonName = birdData.name ?? string.Empty,
                ScientificName = birdData.species ?? string.Empty,
                Tagline = birdData.tagline ?? string.Empty,
                Description = birdData.description ?? string.Empty,
                LovedBy = birdData.loved_count ?? 0,
                SupportedBy = birdData.supported_count ?? 0,
                ImageS3Key = birdData.image_url
            };

            // Generate download URL for image
            try
            {
                if (!string.IsNullOrWhiteSpace(birdData.image_url))
                {
                    dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(birdData.image_url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate download URL for bird {BirdId}", id);
            }

            // Check if current user has loved this bird
            var userId = GetUserIdClaim();
            if (userId.HasValue)
            {
                var loveSql = "SELECT EXISTS(SELECT 1 FROM loves WHERE user_id = @UserId AND bird_id = @BirdId)";
                dto.IsLoved = await connection.ExecuteScalarAsync<bool>(loveSql, new { UserId = userId.Value, BirdId = id });
            }
            else
            {
                dto.IsLoved = false;
            }

            // Map owner summary
            if (birdData.owner_user_id != null)
            {
                dto.Owner = new UserSummaryDto 
                { 
                    UserId = (Guid)birdData.owner_user_id, 
                    Name = birdData.owner_name ?? string.Empty
                };
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

            // Validate S3 key
            if (string.IsNullOrWhiteSpace(dto.ImageS3Key))
            {
                return BadRequest(new { message = "ImageS3Key is required" });
            }

            // Verify file exists in S3
            var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);

            if (!imageExists)
            {
                return BadRequest(new { message = "Bird profile image not found in S3. Please upload the file first." });
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
                CreatedAt = DateTime.UtcNow
            };

            // Use Dapper with all required columns
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO birds (
                    bird_id, owner_id, name, species, tagline, description, image_url, created_at, 
                    loved_count, supported_count, donation_cents, 
                    is_premium, is_memorial, max_media_count
                )
                VALUES (
                    @BirdId, @OwnerId, @Name, @Species, @Tagline, @Description, @ImageUrl, @CreatedAt, 
                    0, 0, 0,
                    false, false, 10
                )",
                new
                {
                    bird.BirdId,
                    bird.OwnerId,
                    bird.Name,
                    bird.Species,
                    bird.Tagline,
                    bird.Description,
                    bird.ImageUrl,
                    bird.CreatedAt
                });

            _logger.LogInformation("Bird created: {BirdId} by user {UserId}", bird.BirdId, userId.Value);

            // Generate download URL for response
            string? imageUrl = null;
            try
            {
                imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate download URL for bird {BirdId}", bird.BirdId);
            }

            var summary = new BirdSummaryDto
            {
                BirdId = bird.BirdId,
                Name = bird.Name,
                Species = bird.Species,
                ImageS3Key = bird.ImageUrl,
                ImageUrl = imageUrl,
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

            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            // Get current bird data
            var bird = await connection.QueryFirstOrDefaultAsync<Bird>(
                "SELECT * FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            if (bird == null) return NotFound();

            // Update image if provided and different
            string? imageToDelete = null;
            var newImageUrl = bird.ImageUrl;
            
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key) && dto.ImageS3Key != bird.ImageUrl)
            {
                var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                if (!imageExists)
                {
                    return BadRequest(new { message = "Bird profile image not found in S3." });
                }

                // Mark old image for deletion
                if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
                {
                    imageToDelete = bird.ImageUrl;
                }

                newImageUrl = dto.ImageS3Key;
            }

            // Update bird with Dapper
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET name = @Name, 
                    species = @Species, 
                    description = @Description, 
                    tagline = @Tagline, 
                    image_url = @ImageUrl
                WHERE bird_id = @BirdId",
                new
                {
                    Name = dto.Name,
                    Species = dto.Species,
                    Description = dto.Description,
                    Tagline = dto.Tagline,
                    ImageUrl = newImageUrl,
                    BirdId = id
                });

            // Delete old image after successful update
            if (imageToDelete != null)
            {
                try
                {
                    await _s3Service.DeleteFileAsync(imageToDelete);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old bird image");
                }
            }
            
            _logger.LogInformation("Bird updated: {BirdId}", id);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            // Get bird before deleting
            var bird = await connection.QueryFirstOrDefaultAsync<Bird>(
                "SELECT * FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            if (bird == null) return NotFound();

            // Delete bird from database
            await connection.ExecuteAsync(
                "DELETE FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });

            // Delete media file from S3
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
            
            _logger.LogInformation("Bird deleted: {BirdId}", id);
            
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/love")]
        public async Task<IActionResult> Love(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get bird
            var bird = await connection.QueryFirstOrDefaultAsync<Bird>(
                "SELECT * FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            if (bird == null) return NotFound("Bird not found");

            // Get user
            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE user_id = @UserId",
                new { UserId = userId });
                
            if (user == null) return Unauthorized("User not found");

            // Check if already loved
            var exists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM loves WHERE user_id = @UserId AND bird_id = @BirdId)",
                new { UserId = userId, BirdId = id });
                
            if (exists) return BadRequest("Already loved");

            // Insert love record
            await connection.ExecuteAsync(@"
                INSERT INTO loves (user_id, bird_id)
                VALUES (@UserId, @BirdId)",
                new { UserId = userId, BirdId = id });

            // Atomic increment of loved_count
            await connection.ExecuteAsync(
                "UPDATE birds SET loved_count = loved_count + 1 WHERE bird_id = @BirdId",
                new { BirdId = id });

            // Get updated love count
            var loveCount = await connection.ExecuteScalarAsync<int>(
                "SELECT loved_count FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });

            // Send notification to bird owner
            if (bird.OwnerId != userId)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
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

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if love exists
            var exists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM loves WHERE user_id = @UserId AND bird_id = @BirdId)",
                new { UserId = userId, BirdId = id });
                
            if (!exists) return NotFound();

            // Delete love record
            await connection.ExecuteAsync(@"
                DELETE FROM loves 
                WHERE user_id = @UserId AND bird_id = @BirdId",
                new { UserId = userId, BirdId = id });

            // Atomic decrement but not below zero
            await connection.ExecuteAsync(
                "UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}/love")]
        public async Task<IActionResult> UnloveDelete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if love exists
            var exists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM loves WHERE user_id = @UserId AND bird_id = @BirdId)",
                new { UserId = userId, BirdId = id });
                
            if (!exists) return NotFound("Love record not found");

            // Verify bird exists
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });
                
            if (!birdExists) return NotFound("Bird not found");

            // Delete love record
            await connection.ExecuteAsync(@"
                DELETE FROM loves 
                WHERE user_id = @UserId AND bird_id = @BirdId",
                new { UserId = userId, BirdId = id });

            // Atomic decrement but not below zero
            await connection.ExecuteAsync(
                "UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            return Ok();
        }

        [HttpGet("{id}/lovers")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> Lovers(Guid id)
        {
            var sql = @"
                SELECT 
                    l.user_id,
                    COALESCE(u.name, '')  name
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

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check for existing active subscription
            var existingCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM bird_premium_subscriptions WHERE bird_id = @BirdId AND status = 'active'",
                new { BirdId = id });
            
            if (existingCount > 0) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;
            var subscriptionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var periodEnd = now.AddMonths(1);

            // Insert subscription
            await connection.ExecuteAsync(@"
                INSERT INTO bird_premium_subscriptions 
                    (id, bird_id, owner_id, status, plan, provider, provider_subscription_id, 
                     price_cents, duration_days, started_at, current_period_end, created_at, updated_at)
                VALUES (@Id, @BirdId, @OwnerId, @Status, @Plan, @Provider, @ProviderSubscriptionId, 
                        @PriceCents, @DurationDays, @StartedAt, @CurrentPeriodEnd, @CreatedAt, @UpdatedAt)",
                new
                {
                    Id = subscriptionId,
                    BirdId = id,
                    OwnerId = userId,
                    Status = "active",
                    Plan = "monthly",
                    Provider = "local",
                    ProviderSubscriptionId = Guid.NewGuid().ToString(),
                    PriceCents = 300,
                    DurationDays = 30,
                    StartedAt = now,
                    CurrentPeriodEnd = periodEnd,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            // Update bird
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET is_premium = true, 
                    premium_plan = @Plan, 
                    premium_expires_at = @ExpiresAt, 
                    max_media_count = @MaxMediaCount
                WHERE bird_id = @BirdId",
                new
                {
                    Plan = "monthly",
                    ExpiresAt = periodEnd,
                    MaxMediaCount = 20,
                    BirdId = id
                });

            return Ok(new { subscriptionId, expiry = periodEnd });
        }

        [Authorize]
        [HttpPost("{id}/premium/subscribe/lifetime")]
        public async Task<IActionResult> PurchaseLifetime(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check for existing active subscription
            var existingCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM bird_premium_subscriptions WHERE bird_id = @BirdId AND status = 'active'",
                new { BirdId = id });
            
            if (existingCount > 0) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;
            var subscriptionId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Insert subscription
            await connection.ExecuteAsync(@"
                INSERT INTO bird_premium_subscriptions 
                    (id, bird_id, owner_id, status, plan, provider, provider_subscription_id, 
                     price_cents, duration_days, started_at, current_period_end, created_at, updated_at)
                VALUES (@Id, @BirdId, @OwnerId, @Status, @Plan, @Provider, @ProviderSubscriptionId, 
                        @PriceCents, @DurationDays, @StartedAt, @CurrentPeriodEnd, @CreatedAt, @UpdatedAt)",
                new
                {
                    Id = subscriptionId,
                    BirdId = id,
                    OwnerId = userId,
                    Status = "active",
                    Plan = "lifetime",
                    Provider = "local",
                    ProviderSubscriptionId = Guid.NewGuid().ToString(),
                    PriceCents = 7000,
                    DurationDays = int.MaxValue,
                    StartedAt = now,
                    CurrentPeriodEnd = DateTime.MaxValue,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            // Update bird
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET is_premium = true, 
                    premium_plan = @Plan, 
                    premium_expires_at = NULL, 
                    max_media_count = @MaxMediaCount
                WHERE bird_id = @BirdId",
                new
                {
                    Plan = "lifetime",
                    MaxMediaCount = 50,
                    BirdId = id
                });

            return Ok(new { subscriptionId, plan = "lifetime" });
        }

        [Authorize]
        [HttpPatch("{id}/premium/style")]
        public async Task<IActionResult> UpdateStyle(Guid id, [FromBody] PremiumStyleDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });
                
            if (!birdExists) return NotFound();

            // Check for active subscription
            var activeCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM bird_premium_subscriptions WHERE bird_id = @BirdId AND status = 'active'",
                new { BirdId = id });
            
            if (activeCount == 0) return Forbid("No active subscription");

            var json = JsonSerializer.Serialize(dto);
            
            // Update premium style
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET premium_style_json = @StyleJson
                WHERE bird_id = @BirdId",
                new { StyleJson = json, BirdId = id });

            return Ok();
        }

        [Authorize]
        [HttpPatch("{id}/premium/qr")]
        public async Task<IActionResult> UpdateQr(Guid id, [FromBody] string qrUrl)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get bird to check premium status
            var bird = await connection.QueryFirstOrDefaultAsync<Bird>(
                "SELECT * FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            if (bird == null) return NotFound();

            // Allow qr to be set only if premium
            if (!bird.IsPremium) return Forbid("Only premium birds can have QR codes");

            // Update QR code
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET qr_code_url = @QrUrl
                WHERE bird_id = @BirdId",
                new { QrUrl = qrUrl, BirdId = id });

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/donate")]
        public async Task<IActionResult> DonateToBird(Guid id, [FromBody] long cents)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var supporterId)) return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get bird
            var bird = await connection.QueryFirstOrDefaultAsync<Bird>(
                "SELECT * FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            if (bird == null) return NotFound("Bird not found");

            // Check if bird is memorial - prevent donations to deceased birds
            if (bird.IsMemorial)
            {
                return BadRequest(new
                {
                    error = "memorial_bird",
                    message = "This bird has passed away and is no longer accepting donations. You can leave a memorial message instead.",
                    canLeaveMessage = true
                });
            }

            // Create transaction record
            var transactionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            
            await connection.ExecuteAsync(@"
                INSERT INTO support_transactions (transaction_id, bird_id, supporter_id, amount, message, created_at)
                VALUES (@TransactionId, @BirdId, @SupporterId, @Amount, NULL, @CreatedAt)",
                new
                {
                    TransactionId = transactionId,
                    BirdId = id,
                    SupporterId = supporterId,
                    Amount = cents / 100m,
                    CreatedAt = now
                });

            // Update bird donation amount and get count
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET donation_cents = donation_cents + @Cents
                WHERE bird_id = @BirdId",
                new { Cents = cents, BirdId = id });
            
            var supportedCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM support_transactions WHERE bird_id = @BirdId",
                new { BirdId = id });
                
            await connection.ExecuteAsync(@"
                UPDATE birds 
                SET supported_count = @SupportedCount
                WHERE bird_id = @BirdId",
                new { SupportedCount = supportedCount, BirdId = id });

            // Get updated donation total
            var totalDonated = await connection.ExecuteScalarAsync<long>(
                "SELECT donation_cents FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });

            // Send notification to bird owner
            if (bird.OwnerId != supporterId)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var supporter = await connection.QueryFirstOrDefaultAsync<User>(
                            "SELECT * FROM users WHERE user_id = @UserId",
                            new { UserId = supporterId });
                            
                        var amountDisplay = (cents / 100m).ToString("F2");

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = bird.OwnerId,
                            Type = NotificationType.BirdSupported,
                            Title = "Corn " + (supporter?.Name ?? "Someone") + " supported " + bird.Name + "!",
                            Message = $"{supporter?.Name ?? "Someone"} contributed ${amountDisplay} to support {bird.Name}. Total: ${totalDonated / 100m:F2}",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = $"/birds/{id}",
                            BirdId = id,
                            TransactionId = transactionId,
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
                            DeepLink = $"/support/{transactionId}",
                            BirdId = id,
                            TransactionId = transactionId
                        });
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            return Ok(new { totalDonated });
        }

        // ============================================================================
        // Memorial Bird Endpoints
        // ============================================================================

        /// <summary>
        /// Mark a bird as memorial (deceased). Only the bird owner can perform this action.
        /// POST /api/birds/{id}/memorial
        /// </summary>
        [Authorize]
        [HttpPost("{id}/memorial")]
        public async Task<ActionResult<MarkMemorialResponseDto>> MarkAsMemorial(Guid id, [FromBody] MemorialRequestDto request)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var result = await _memorialService.MarkBirdAsMemorialAsync(id, userId.Value, request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get memorial details for a deceased bird
        /// GET /api/birds/{id}/memorial
        /// </summary>
        [HttpGet("{id}/memorial")]
        public async Task<ActionResult<MemorialBirdDto>> GetMemorial(Guid id)
        {
            var memorial = await _memorialService.GetMemorialDetailsAsync(id);

            if (memorial == null)
            {
                return NotFound(new { message = "Memorial bird not found" });
            }

            return Ok(memorial);
        }

        /// <summary>
        /// Add a condolence/tribute message to a memorial bird
        /// POST /api/birds/{id}/memorial/messages
        /// </summary>
        [Authorize]
        [HttpPost("{id}/memorial/messages")]
        public async Task<ActionResult<MemorialMessageDto>> AddMemorialMessage(Guid id, [FromBody] CreateMemorialMessageDto message)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            try
            {
                var messageDto = await _memorialService.AddMemorialMessageAsync(id, userId.Value, message);
                return CreatedAtAction(nameof(GetMemorialMessages), new { id = id }, messageDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get paginated memorial messages for a bird
        /// GET /api/birds/{id}/memorial/messages?page=1&pageSize=20&sortBy=recent
        /// </summary>
        [HttpGet("{id}/memorial/messages")]
        public async Task<ActionResult<MemorialMessagePageDto>> GetMemorialMessages(
            Guid id, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "recent")
        {
            var messages = await _memorialService.GetMemorialMessagesAsync(id, page, pageSize, sortBy);
            return Ok(messages);
        }

        /// <summary>
        /// Delete a memorial message (only bird owner or message author)
        /// DELETE /api/birds/{id}/memorial/messages/{messageId}
        /// </summary>
        [Authorize]
        [HttpDelete("{id}/memorial/messages/{messageId}")]
        public async Task<IActionResult> DeleteMemorialMessage(Guid id, Guid messageId)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var deleted = await _memorialService.DeleteMemorialMessageAsync(messageId, userId.Value);

            if (!deleted)
            {
                return NotFound(new { message = "Memorial message not found or you don't have permission to delete it" });
            }

            return Ok(new { success = true, message = "Memorial message deleted" });
        }

        /// <summary>
        /// Process memorial fund redirection (Admin only)
        /// POST /api/birds/{id}/memorial/process-funds
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/memorial/process-funds")]
        public async Task<ActionResult<MemorialFundRedirectionDto>> ProcessMemorialFunds(Guid id)
        {
            try
            {
                var result = await _memorialService.ProcessFundRedirectionAsync(id);
                return Ok(new { success = true, redirection = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing memorial funds for bird {BirdId}", id);
                return StatusCode(500, new { message = "An error occurred while processing memorial funds" });
            }
        }
    }
}
