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
        private readonly IContentModerationService _moderationService;
        private readonly IBirdActivityService _activityService;
        private readonly IBirdFollowService _birdFollowService;
        private readonly ICaretakerEligibilityService _eligibilityService;

        // Anti-abuse: Maximum birds per user
        private const int MaxBirdsPerUser = 10;

        public BirdsController(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IMapper mapper,
            INotificationService notificationService,
            IS3Service s3Service,
            ILogger<BirdsController> logger,
            IMemorialService memorialService,
            IContentModerationService moderationService,
            IBirdActivityService activityService,
            IBirdFollowService birdFollowService,
            ICaretakerEligibilityService eligibilityService)
        {
            _db = db;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _notificationService = notificationService;
            _s3Service = s3Service;
            _logger = logger;
            _memorialService = memorialService;
            _moderationService = moderationService;
            _activityService = activityService;
            _birdFollowService = birdFollowService;
            _eligibilityService = eligibilityService;
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
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // Clamp page size to reasonable limits
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);
            var offset = (page - 1) * pageSize;

            var userId = GetUserIdClaim();

            // Get all bird IDs this user has loved (if authenticated)
            HashSet<Guid> lovedBirdIds = new HashSet<Guid>();
            if (userId.HasValue)
            {
                var sql = "SELECT bird_id FROM loves WHERE user_id = @UserId";
                var lovedIds = await _dbFactory.QueryListAsync<Guid>(sql, new { UserId = userId.Value });
                lovedBirdIds = new HashSet<Guid>(lovedIds);
            }

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get total count for pagination (only public birds)
            var countSql = "SELECT COUNT(*) FROM birds WHERE owner_id IS NOT NULL AND bird_id IS NOT NULL AND is_public = TRUE";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            // Get paginated birds with activity tracking fields - ordered by most recent activity
            // Only show public birds (is_public = TRUE)
            var birdsSql = @"
                SELECT
                    bird_id,
                    name,
                    species,
                    image_url,
                    tagline,
                    COALESCE(loved_count, 0) loved_count,
                    COALESCE(supported_count, 0) supported_count,
                    owner_id,
                    last_activity_at,
                    is_memorial,
                    is_public
                FROM birds
                WHERE owner_id IS NOT NULL AND bird_id IS NOT NULL AND is_public = TRUE
                ORDER BY COALESCE(last_activity_at, created_at) DESC
                LIMIT @PageSize OFFSET @Offset";

            var birds = await connection.QueryAsync<dynamic>(birdsSql, new { PageSize = pageSize, Offset = offset });

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
                DateTime? lastActivityAt = bird.last_activity_at as DateTime?;
                bool isMemorial = bird.is_memorial ?? false;

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

                // Calculate activity status
                var activityStatus = _activityService.GetActivityStatus(lastActivityAt, isMemorial);
                var canSupport = _activityService.CanReceiveSupport(activityStatus);
                var lastSeenText = _activityService.GetLastSeenText(lastActivityAt, isMemorial);

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
                    IsLoved = lovedBirdIds.Contains(birdId),
                    ActivityStatus = activityStatus,
                    LastSeenText = lastSeenText,
                    CanSupport = canSupport,
                    IsMemorial = isMemorial
                });
            }

            // Return paginated response
            return Ok(new
            {
                items = birdDtos,
                page,
                pageSize,
                totalCount
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BirdProfileDto>> Get(Guid id)
        {
            var userId = GetUserIdClaim();

            // Calculate week start for weekly support tracking
            var weekStart = GetWeekStart(DateTime.UtcNow);

            // Get bird with owner using raw SQL (including activity tracking and visibility fields)
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
                    b.last_activity_at,
                    b.is_memorial,
                    b.is_public,
                    b.needs_support,
                    u.user_id owner_user_id,
                    u.name owner_name,
                    COALESCE(r.times_supported, 0) times_supported_this_week
                FROM birds b
                LEFT JOIN users u ON b.owner_id = u.user_id
                LEFT JOIN weekly_bird_support_rounds r
                    ON b.bird_id = r.bird_id AND r.week_start_date = @WeekStart::date
                WHERE b.bird_id = @BirdId";

            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            var birdData = await connection.QueryFirstOrDefaultAsync<dynamic>(birdSql, new { BirdId = id, WeekStart = weekStart.ToString("yyyy-MM-dd") });

            if (birdData == null) return NotFound();

            // Check visibility - hidden birds only visible to owner
            bool isPublic = birdData.is_public ?? true;
            Guid ownerId = birdData.owner_id;
            if (!isPublic && (!userId.HasValue || userId.Value != ownerId))
            {
                return NotFound(); // Hidden bird - appear as not found to non-owners
            }

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

            // Extract activity tracking fields
            DateTime? lastActivityAt = birdData.last_activity_at as DateTime?;
            bool isMemorial = birdData.is_memorial ?? false;

            // Calculate activity status
            var activityStatus = _activityService.GetActivityStatus(lastActivityAt, isMemorial);
            var canSupport = _activityService.CanReceiveSupport(activityStatus);
            var lastSeenText = _activityService.GetLastSeenText(lastActivityAt, isMemorial);
            var supportUnavailableMessage = _activityService.GetSupportUnavailableMessage(activityStatus);

            // Get weekly support tracking data
            int timesSupportedThisWeek = (int)(birdData.times_supported_this_week ?? 0);
            bool needsSupport = birdData.needs_support ?? false;

            // Map to DTO (using property names from BirdProfileDto)
            var dto = new BirdProfileDto
            {
                BirdId = id,
                CommonName = birdData.name ?? string.Empty,
                ScientificName = birdData.species ?? string.Empty,
                Tagline = birdData.tagline ?? string.Empty,
                Description = birdData.description ?? string.Empty,
                LovedBy = birdData.loved_count ?? 0,
                SupportedBy = birdData.supported_count ?? 0,
                ImageS3Key = birdData.image_url,
                ActivityStatus = activityStatus,
                LastSeenText = lastSeenText,
                CanSupport = canSupport,
                SupportUnavailableMessage = supportUnavailableMessage,
                IsMemorial = isMemorial,
                TimesSupportedThisWeek = timesSupportedThisWeek,
                // Only show visibility status to owner
                IsPublic = userId.HasValue && userId.Value == ownerId ? isPublic : null,
                NeedsSupport = userId.HasValue && userId.Value == ownerId ? needsSupport : null
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

        /// <summary>
        /// Get paginated stories for a specific bird
        /// </summary>
        [HttpGet("{id}/stories")]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> GetBirdStories(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists
            var birdExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM birds WHERE bird_id = @BirdId",
                new { BirdId = id });

            if (birdExists == 0)
            {
                return NotFound(new { message = "Bird not found" });
            }

            // Get total count for this bird
            var total = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM stories WHERE bird_id = @BirdId",
                new { BirdId = id });

            // Get stories for this bird
            var offset = (page - 1) * pageSize;
            var sql = @"
                SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                       b.name as bird_name
                FROM stories s
                JOIN birds b ON s.bird_id = b.bird_id
                WHERE s.bird_id = @BirdId
                ORDER BY s.created_at DESC
                LIMIT @PageSize OFFSET @Offset";

            var stories = await connection.QueryAsync<dynamic>(sql, new
            {
                BirdId = id,
                PageSize = pageSize,
                Offset = offset
            });

            var items = new List<StorySummaryDto>();
            foreach (var story in stories)
            {
                string content = story.content ?? string.Empty;
                string birdName = story.bird_name ?? string.Empty;

                var dto = new StorySummaryDto
                {
                    StoryId = (Guid)story.story_id,
                    Birds = string.IsNullOrWhiteSpace(birdName)
                        ? new List<string>()
                        : new List<string> { birdName },
                    Mode = (StoryMode?)story.mode,
                    Date = ((DateTime)story.created_at).ToString("MMMM d, yyyy"),
                    Preview = content.Length > 140 ? content.Substring(0, 140) + "..." : content,
                    ImageS3Key = story.image_url,
                    VideoS3Key = story.video_url,
                    CreatedAt = story.created_at
                };

                // Generate download URLs
                if (!string.IsNullOrWhiteSpace(story.image_url))
                {
                    try
                    {
                        dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(story.image_url);
                    }
                    catch { }
                }

                if (!string.IsNullOrWhiteSpace(story.video_url))
                {
                    try
                    {
                        dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(story.video_url);
                    }
                    catch { }
                }

                items.Add(dto);
            }

            return Ok(new PagedResult<StorySummaryDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Upload a bird profile image before creating the bird
        /// </summary>
        /// <remarks>
        /// Use this endpoint to upload an image first, then include the returned S3 key
        /// in the POST /api/birds request to create the bird with the image.
        /// </remarks>
        [Authorize]
        [HttpPost("upload-image")]
        [ProducesResponseType(typeof(BirdImageUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BirdImageUploadResponse>> UploadImageBeforeCreate(IFormFile file)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Invalid file type. Allowed: jpeg, png, gif, webp" });
            }

            // Validate file size (max 10MB)
            const long maxSize = 10 * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return BadRequest(new { message = "File too large. Maximum size is 10MB" });
            }

            // Get file extension
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }

            try
            {
                // Generate S3 key for bird profile image (use userId as placeholder since bird doesn't exist yet)
                var (_, s3Key) = await _s3Service.GenerateUploadUrlAsync(
                    userId.Value,
                    "bird-profile-image",
                    extension,
                    null);

                // Upload file directly to S3
                using var stream = file.OpenReadStream();
                await _s3Service.UploadFileAsync(s3Key, stream, file.ContentType);

                // Generate download URL for immediate use
                var downloadUrl = await _s3Service.GenerateDownloadUrlAsync(s3Key);

                _logger.LogInformation("Bird image uploaded before creation: {S3Key} by user {UserId}", s3Key, userId.Value);

                return Ok(new BirdImageUploadResponse
                {
                    S3Key = s3Key,
                    Url = downloadUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading bird image for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "Failed to upload image" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BirdSummaryDto>> Post([FromBody] BirdCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Anti-abuse: Check max birds per user
            // Invariant: Birds never multiply money. One user = one wallet = capped baseline support.
            var canAddMore = await _eligibilityService.CanAddMoreBirdsAsync(userId.Value, MaxBirdsPerUser);
            if (!canAddMore)
            {
                var currentCount = await _eligibilityService.GetBirdCountAsync(userId.Value);
                _logger.LogWarning(
                    "User {UserId} attempted to create bird but has reached max limit ({Max}). Current: {Current}",
                    userId.Value, MaxBirdsPerUser, currentCount);
                return BadRequest(new
                {
                    message = $"Maximum of {MaxBirdsPerUser} birds allowed per account",
                    code = CaretakerEligibilityErrorCodes.MaxBirdsReached,
                    currentCount,
                    maxAllowed = MaxBirdsPerUser
                });
            }

            // Validate S3 key if provided
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key))
            {
                var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                if (!imageExists)
                {
                    return BadRequest(new { message = "Bird profile image not found in S3. Please upload the file first." });
                }
            }

            // Content moderation check
            var moderationResult = await _moderationService.ModerateBirdProfileAsync(
                dto.Name,
                dto.Species,
                dto.Tagline,
                dto.Description,
                dto.ImageS3Key);

            if (moderationResult.IsBlocked)
            {
                _logger.LogWarning("Bird profile blocked by moderation for user {UserId}. Reason: {Reason}",
                    userId.Value, moderationResult.BlockReason);
                return BadRequest(new
                {
                    message = "Content blocked by moderation",
                    reason = moderationResult.BlockReason,
                    code = "CONTENT_MODERATION_BLOCKED"
                });
            }

            var bird = new Bird
            {
                BirdId = Guid.NewGuid(),
                OwnerId = userId.Value,
                Name = dto.Name,
                Species = dto.Species,
                Tagline = dto.Tagline,
                Description = dto.Description,
                Location = dto.Location,
                Age = dto.Age,
                ImageUrl = dto.ImageS3Key,
                CreatedAt = DateTime.UtcNow
            };

            // Use Dapper with all required columns (all birds are equal - same media limit)
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO birds (
                    bird_id, owner_id, name, species, tagline, description, location, age, image_url, created_at,
                    loved_count, supported_count, donation_cents,
                    is_memorial, max_media_count, last_activity_at
                )
                VALUES (
                    @BirdId, @OwnerId, @Name, @Species, @Tagline, @Description, @Location, @Age, @ImageUrl, @CreatedAt,
                    0, 0, 0,
                    false, 10, @CreatedAt
                )",
                new
                {
                    bird.BirdId,
                    bird.OwnerId,
                    bird.Name,
                    bird.Species,
                    bird.Tagline,
                    bird.Description,
                    bird.Location,
                    bird.Age,
                    bird.ImageUrl,
                    bird.CreatedAt
                });

            _logger.LogInformation("Bird created: {BirdId} by user {UserId}", bird.BirdId, userId.Value);

            // Generate download URL for response (only if image was provided)
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(bird.ImageUrl))
            {
                try
                {
                    imageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for bird {BirdId}", bird.BirdId);
                }
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
                IsLoved = false,
                ActivityStatus = Models.Enums.BirdActivityStatus.Active,
                LastSeenText = "Recently active",
                CanSupport = true,
                IsMemorial = false
            };

            return CreatedAtAction(nameof(Get), new { id = bird.BirdId }, summary);
        }

        /// <summary>
        /// Update a bird's profile
        /// </summary>
        /// <remarks>
        /// Upload image first using POST /api/birds/{id}/image, then include the S3 key here.
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(Guid id, [FromBody] BirdUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
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

            // Content moderation check for updates
            var moderationResult = await _moderationService.ModerateBirdProfileAsync(
                dto.Name,
                dto.Species,
                null, // tagline not editable in update
                dto.Description,
                newImageUrl != bird.ImageUrl ? newImageUrl : null);

            if (moderationResult.IsBlocked)
            {
                _logger.LogWarning("Bird profile update blocked by moderation for bird {BirdId}. Reason: {Reason}",
                    id, moderationResult.BlockReason);
                return BadRequest(new
                {
                    message = "Content blocked by moderation",
                    reason = moderationResult.BlockReason,
                    code = "CONTENT_MODERATION_BLOCKED"
                });
            }

            // Update bird with Dapper (including location and age)
            await connection.ExecuteAsync(@"
                UPDATE birds
                SET name = @Name,
                    species = @Species,
                    description = @Description,
                    location = @Location,
                    age = @Age,
                    image_url = @ImageUrl
                WHERE bird_id = @BirdId",
                new
                {
                    Name = dto.Name,
                    Species = dto.Species,
                    Description = dto.Description,
                    Location = dto.Location,
                    Age = dto.Age,
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

            // Update bird's last activity timestamp
            await _activityService.UpdateLastActivityAsync(id);

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

        /// <summary>
        /// Update QR code URL for a bird (all birds are equal - QR available to all)
        /// </summary>
        [Authorize]
        [HttpPatch("{id}/qr")]
        public async Task<IActionResult> UpdateQr(Guid id, [FromBody] string qrUrl)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });

            if (!birdExists) return NotFound();

            // Update QR code - all birds can have QR codes
            await connection.ExecuteAsync(@"
                UPDATE birds
                SET qr_code_url = @QrUrl
                WHERE bird_id = @BirdId",
                new { QrUrl = qrUrl, BirdId = id });

            return Ok();
        }

        /// <summary>
        /// Toggle whether a bird can receive support
        /// </summary>
        /// <remarks>
        /// Bird owners can enable/disable support for their birds.
        /// When disabled, the bird will not appear in support flows.
        /// Note: Owner must also have a wallet configured for support to work.
        /// </remarks>
        [Authorize]
        [HttpPatch("{id}/support-settings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSupportSettings(Guid id, [FromBody] BirdSupportSettingsDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });

            if (!birdExists) return NotFound();

            // Update support_enabled
            await connection.ExecuteAsync(@"
                UPDATE birds
                SET support_enabled = @SupportEnabled
                WHERE bird_id = @BirdId",
                new { dto.SupportEnabled, BirdId = id });

            _logger.LogInformation(
                "Bird {BirdId} support settings updated: SupportEnabled={SupportEnabled}",
                id, dto.SupportEnabled);

            return Ok(new {
                success = true,
                supportEnabled = dto.SupportEnabled,
                message = dto.SupportEnabled
                    ? "This bird can now receive support"
                    : "Support has been disabled for this bird"
            });
        }

        /// <summary>
        /// Update bird visibility (public/hidden)
        /// </summary>
        /// <remarks>
        /// Hide a bird from public listings or make it visible again.
        /// When hidden (isPublic=false), the bird is in draft mode and only visible to the owner.
        /// Hidden birds won't appear in searches, feeds, or public profiles.
        /// </remarks>
        [Authorize]
        [HttpPatch("{id}/visibility")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateVisibility(Guid id, [FromBody] BirdVisibilityDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if bird exists
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });

            if (!birdExists) return NotFound();

            // Update is_public
            await connection.ExecuteAsync(@"
                UPDATE birds
                SET is_public = @IsPublic
                WHERE bird_id = @BirdId",
                new { dto.IsPublic, BirdId = id });

            _logger.LogInformation(
                "Bird {BirdId} visibility updated: IsPublic={IsPublic}",
                id, dto.IsPublic);

            return Ok(new {
                success = true,
                isPublic = dto.IsPublic,
                message = dto.IsPublic
                    ? "This bird is now visible to everyone"
                    : "This bird is now hidden from public"
            });
        }

        /// <summary>
        /// Upload a profile image for a bird (multipart/form-data)
        /// </summary>
        /// <remarks>
        /// Returns the S3 key and URL for the uploaded image.
        /// Use the S3 key in PUT /api/birds/{id} to update the bird's profile.
        /// </remarks>
        [Authorize]
        [HttpPost("{id}/image")]
        [ProducesResponseType(typeof(BirdImageUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BirdImageUploadResponse>> UploadBirdImage(Guid id, IFormFile file)
        {
            if (!await EnsureOwner(id)) return Forbid();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Invalid file type. Allowed: jpeg, png, gif, webp" });
            }

            // Validate file size (max 10MB)
            const long maxSize = 10 * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return BadRequest(new { message = "File too large. Maximum size is 10MB" });
            }

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Get file extension
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }

            try
            {
                // Generate S3 key for bird profile image
                var (_, s3Key) = await _s3Service.GenerateUploadUrlAsync(
                    userId.Value,
                    "bird-profile-image",
                    extension,
                    id);

                // Upload file directly to S3
                using var stream = file.OpenReadStream();
                await _s3Service.UploadFileAsync(s3Key, stream, file.ContentType);

                // Generate download URL for immediate use
                var downloadUrl = await _s3Service.GenerateDownloadUrlAsync(s3Key);

                _logger.LogInformation("Bird image uploaded: {S3Key} for bird {BirdId}", s3Key, id);

                return Ok(new BirdImageUploadResponse
                {
                    S3Key = s3Key,
                    Url = downloadUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading bird image for bird {BirdId}", id);
                return StatusCode(500, new { message = "Failed to upload image" });
            }
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

            // Check activity status - prevent donations to inactive or memorial birds
            var activityStatus = _activityService.GetActivityStatus(bird.LastActivityAt, bird.IsMemorial);
            var canSupport = _activityService.CanReceiveSupport(activityStatus);

            if (!canSupport)
            {
                var supportUnavailableMessage = _activityService.GetSupportUnavailableMessage(activityStatus);

                if (bird.IsMemorial)
                {
                    return BadRequest(new
                    {
                        error = "memorial_bird",
                        message = "This bird has passed away and is no longer accepting donations. You can leave a memorial message instead.",
                        canLeaveMessage = true,
                        activityStatus = activityStatus.ToString()
                    });
                }

                return BadRequest(new
                {
                    error = "inactive_bird",
                    message = supportUnavailableMessage ?? "Support is currently unavailable for this bird.",
                    activityStatus = activityStatus.ToString(),
                    lastSeenText = _activityService.GetLastSeenText(bird.LastActivityAt, bird.IsMemorial)
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

        // ============================================================================
        // Bird Follow Endpoints (Smart Feed)
        // ============================================================================

        /// <summary>
        /// Follow a bird to see their stories in your feed.
        /// POST /api/birds/{id}/follow
        /// </summary>
        [Authorize]
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> FollowBird(Guid id)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Check if bird exists
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            var birdExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId)",
                new { BirdId = id });

            if (!birdExists) return NotFound("Bird not found");

            var result = await _birdFollowService.FollowBirdAsync(userId.Value, id);

            if (!result)
            {
                return BadRequest(new { message = "Already following this bird" });
            }

            _logger.LogInformation("User {UserId} followed bird {BirdId}", userId.Value, id);
            return Ok(new { message = "Now following this bird", isFollowing = true });
        }

        /// <summary>
        /// Unfollow a bird.
        /// DELETE /api/birds/{id}/follow
        /// </summary>
        [Authorize]
        [HttpDelete("{id}/follow")]
        public async Task<IActionResult> UnfollowBird(Guid id)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var result = await _birdFollowService.UnfollowBirdAsync(userId.Value, id);

            if (!result)
            {
                return NotFound(new { message = "Not following this bird" });
            }

            _logger.LogInformation("User {UserId} unfollowed bird {BirdId}", userId.Value, id);
            return Ok(new { message = "Unfollowed this bird", isFollowing = false });
        }

        /// <summary>
        /// Get all birds the current user is following.
        /// GET /api/birds/following
        /// </summary>
        [Authorize]
        [HttpGet("following")]
        public async Task<ActionResult<List<BirdSummaryDto>>> GetFollowingBirds()
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var followedBirdIds = await _birdFollowService.GetFollowedBirdIdsAsync(userId.Value);

            if (!followedBirdIds.Any())
            {
                return Ok(new List<BirdSummaryDto>());
            }

            // Get followed birds with details
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            var sql = @"
                SELECT
                    bird_id,
                    name,
                    species,
                    image_url,
                    tagline,
                    COALESCE(loved_count, 0) loved_count,
                    COALESCE(supported_count, 0) supported_count,
                    owner_id,
                    last_activity_at,
                    is_memorial
                FROM birds
                WHERE bird_id = ANY(@BirdIds)";

            var birds = await connection.QueryAsync<dynamic>(sql, new { BirdIds = followedBirdIds.ToArray() });

            // Get loved birds for this user
            var lovedSql = "SELECT bird_id FROM loves WHERE user_id = @UserId";
            var lovedIds = await connection.QueryAsync<Guid>(lovedSql, new { UserId = userId.Value });
            var lovedBirdIds = new HashSet<Guid>(lovedIds);

            var birdDtos = new List<BirdSummaryDto>();
            foreach (var bird in birds)
            {
                if (bird.bird_id == null || bird.owner_id == null) continue;

                Guid birdId = (Guid)bird.bird_id;
                Guid ownerId = (Guid)bird.owner_id;
                DateTime? lastActivityAt = bird.last_activity_at as DateTime?;
                bool isMemorial = bird.is_memorial ?? false;

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
                    _logger.LogWarning(ex, "Failed to generate download URL for bird {BirdId}", birdId);
                }

                var activityStatus = _activityService.GetActivityStatus(lastActivityAt, isMemorial);
                var canSupport = _activityService.CanReceiveSupport(activityStatus);
                var lastSeenText = _activityService.GetLastSeenText(lastActivityAt, isMemorial);

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
                    IsLoved = lovedBirdIds.Contains(birdId),
                    ActivityStatus = activityStatus,
                    LastSeenText = lastSeenText,
                    CanSupport = canSupport,
                    IsMemorial = isMemorial
                });
            }

            return Ok(birdDtos);
        }

        /// <summary>
        /// Check if the current user is following a specific bird.
        /// GET /api/birds/{id}/follow
        /// </summary>
        [Authorize]
        [HttpGet("{id}/follow")]
        public async Task<ActionResult<object>> IsFollowingBird(Guid id)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var isFollowing = await _birdFollowService.IsFollowingAsync(userId.Value, id);
            return Ok(new { isFollowing });
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            // Week starts on Sunday
            var diff = date.DayOfWeek - DayOfWeek.Sunday;
            return date.Date.AddDays(-diff);
        }
    }
}
