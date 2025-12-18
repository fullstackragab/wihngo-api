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

    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IS3Service _s3Service;
        private readonly ILogger<StoriesController> _logger;
        private readonly IAiStoryGenerationService _aiStoryService;
        private readonly IWhisperTranscriptionService _whisperService;
        private readonly IContentModerationService _moderationService;
        private readonly IBirdActivityService _activityService;
        private readonly ILanguageDetectionService _languageDetectionService;

        public StoriesController(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IMapper mapper,
            INotificationService notificationService,
            IS3Service s3Service,
            ILogger<StoriesController> logger,
            IAiStoryGenerationService aiStoryService,
            IWhisperTranscriptionService whisperService,
            IContentModerationService moderationService,
            IBirdActivityService activityService,
            ILanguageDetectionService languageDetectionService)
        {
            _db = db;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _notificationService = notificationService;
            _s3Service = s3Service;
            _logger = logger;
            _aiStoryService = aiStoryService;
            _whisperService = whisperService;
            _moderationService = moderationService;
            _activityService = activityService;
            _languageDetectionService = languageDetectionService;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var connection = await _dbFactory.CreateOpenConnectionAsync();
            try
            {
                // Get total count
                var total = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM stories");

                // Get stories with bird name - direct join, no aggregation needed
                var sql = @"
                    SELECT 
                        s.story_id,
                        s.content,
                        s.mode,
                        s.image_url,
                        s.video_url,
                        s.created_at,
                        b.name as bird_name
                    FROM stories s
                    JOIN birds b ON s.bird_id = b.bird_id
                    ORDER BY s.created_at DESC
                    OFFSET @Offset LIMIT @Limit";

                var stories = await connection.QueryAsync<dynamic>(sql, new { Offset = (page - 1) * pageSize, Limit = pageSize });
                var dtoItems = new List<StorySummaryDto>(stories.Count());

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
                        VideoS3Key = story.video_url
                    };

                    // Generate download URLs
                    string? imageUrl = story.image_url;
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        try
                        {
                            dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate download URL for story image {StoryId}", dto.StoryId);
                        }
                    }

                    string? videoUrl = story.video_url;
                    if (!string.IsNullOrWhiteSpace(videoUrl))
                    {
                        try
                        {
                            dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(videoUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", dto.StoryId);
                        }
                    }

                    dtoItems.Add(dto);
                }

                var result = new PagedResult<StorySummaryDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total,
                    Items = dtoItems
                };

                return Ok(result);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Get stories by user (author)
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> GetUserStories(
            Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var connection = await _dbFactory.CreateOpenConnectionAsync();
            try
            {
                // Get total count for this user
                var total = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM stories WHERE author_id = @UserId",
                    new { UserId = userId });

                // Get stories with bird name - direct join
                var sql = @"
                    SELECT 
                        s.story_id,
                        s.content,
                        s.mode,
                        s.image_url,
                        s.video_url,
                        s.created_at,
                        b.name as bird_name
                    FROM stories s
                    JOIN birds b ON s.bird_id = b.bird_id
                    WHERE s.author_id = @UserId
                    ORDER BY s.created_at DESC
                    OFFSET @Offset LIMIT @Limit";

                var stories = await connection.QueryAsync<dynamic>(sql, new { UserId = userId, Offset = (page - 1) * pageSize, Limit = pageSize });
                var dtoItems = new List<StorySummaryDto>(stories.Count());

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
                        VideoS3Key = story.video_url
                    };

                    // Generate download URLs
                    string? imageUrl = story.image_url;
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        try
                        {
                            dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate download URL for story {StoryId}", dto.StoryId);
                        }
                    }

                    string? videoUrl = story.video_url;
                    if (!string.IsNullOrWhiteSpace(videoUrl))
                    {
                        try
                        {
                            dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(videoUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", dto.StoryId);
                        }
                    }

                    dtoItems.Add(dto);
                }

                var result = new PagedResult<StorySummaryDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total,
                    Items = dtoItems
                };

                return Ok(result);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Get current user's stories
        /// </summary>
        [HttpGet("my-stories")]
        [Authorize]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> GetMyStories(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            return await GetUserStories(userId.Value, page, pageSize);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StoryReadDto>> Get(Guid id)
        {
            var connection = await _dbFactory.CreateOpenConnectionAsync();
            try
            {
                // Get story with bird data - direct join, no junction table
                var sql = @"
                    SELECT
                        s.story_id,
                        s.content,
                        s.mode,
                        s.image_url,
                        s.video_url,
                        s.audio_url,
                        s.created_at,
                        s.author_id,
                        u.user_id as author_user_id,
                        u.name as author_name,
                        b.bird_id,
                        b.name as bird_name,
                        b.species,
                        b.image_url as bird_image_url,
                        b.tagline,
                        b.loved_count,
                        b.supported_count,
                        b.owner_id
                    FROM stories s
                    LEFT JOIN users u ON s.author_id = u.user_id
                    LEFT JOIN birds b ON s.bird_id = b.bird_id
                    WHERE s.story_id = @StoryId";

                var story = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { StoryId = id });

                if (story == null) return NotFound();

                // Build DTO
                var dto = new StoryReadDto
                {
                    StoryId = (Guid)story.story_id,
                    Content = story.content ?? string.Empty,
                    Mode = (StoryMode?)story.mode,
                    ImageS3Key = story.image_url,
                    VideoS3Key = story.video_url,
                    AudioS3Key = story.audio_url,
                    CreatedAt = (DateTime)story.created_at,
                    Author = story.author_user_id != null ? new UserSummaryDto
                    {
                        UserId = (Guid)story.author_user_id,
                        Name = story.author_name ?? string.Empty
                    } : new UserSummaryDto(),
                    Birds = new List<BirdSummaryDto>()
                };

                // Add the single bird if it exists
                if (story.bird_id != null)
                {
                    dto.Birds.Add(new BirdSummaryDto
                    {
                        BirdId = (Guid)story.bird_id,
                        Name = story.bird_name ?? string.Empty,
                        Species = story.species,
                        ImageS3Key = story.bird_image_url,
                        Tagline = story.tagline,
                        LovedBy = story.loved_count ?? 0,
                        SupportedBy = story.supported_count ?? 0,
                        OwnerId = (Guid)story.owner_id
                    });
                }

                // Generate download URLs
                if (!string.IsNullOrWhiteSpace(story.image_url))
                {
                    try
                    {
                        dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(story.image_url);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate download URL for story {StoryId}", id);
                    }
                }

                if (!string.IsNullOrWhiteSpace(story.video_url))
                {
                    try
                    {
                        dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(story.video_url);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", id);
                    }
                }

                if (!string.IsNullOrWhiteSpace(story.audio_url))
                {
                    try
                    {
                        dto.AudioUrl = await _s3Service.GenerateDownloadUrlAsync(story.audio_url);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate audio download URL for story {StoryId}", id);
                    }
                }

                // Generate download URL for bird image
                foreach (var bird in dto.Birds)
                {
                    if (!string.IsNullOrWhiteSpace(bird.ImageS3Key))
                    {
                        try
                        {
                            bird.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageS3Key);
                        }
                        catch { }
                    }
                }

                return Ok(dto);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Generate AI-powered story content based on bird context and mood
        /// POST /api/stories/generate
        /// </summary>
        [HttpPost("generate")]
        [Authorize]
        public async Task<ActionResult<GenerateStoryResponseDto>> GenerateStory([FromBody] GenerateStoryRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdClaim();
            if (userId == null)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid or expired token" });
            }

            try
            {
                // Check rate limits
                var isRateLimited = await _aiStoryService.IsRateLimitExceededAsync(userId.Value, request.BirdId);
                if (isRateLimited)
                {
                    return StatusCode(429, new
                    {
                        error = "TooManyRequests",
                        message = "AI generation limit exceeded. Please try again later.",
                        retryAfter = 3600
                    });
                }

                // Verify bird ownership
                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                var birdCheckSql = "SELECT owner_id FROM birds WHERE bird_id = @BirdId";
                var ownerId = await connection.QueryFirstOrDefaultAsync<Guid?>(birdCheckSql, new { BirdId = request.BirdId });

                if (ownerId == null)
                {
                    return NotFound(new { error = "NotFound", message = "Bird not found" });
                }

                if (ownerId.Value != userId.Value)
                {
                    return StatusCode(403, new { error = "Forbidden", message = "You do not own this bird" });
                }

                // Generate story
                var response = await _aiStoryService.GenerateStoryAsync(request, userId.Value);

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("limit exceeded"))
            {
                return StatusCode(429, new
                {
                    error = "TooManyRequests",
                    message = ex.Message,
                    retryAfter = 3600
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
            {
                _logger.LogError(ex, "OpenAI API key not configured");
                return StatusCode(503, new
                {
                    error = "ServiceUnavailable",
                    message = "AI service temporarily unavailable"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI API request failed");
                return StatusCode(503, new
                {
                    error = "ServiceUnavailable",
                    message = "AI service temporarily unavailable"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate story for user {UserId}, bird {BirdId}", userId, request.BirdId);
                return StatusCode(500, new
                {
                    error = "InternalError",
                    message = "Failed to generate story content"
                });
            }
        }

        /// <summary>
        /// Transcribe audio to text using OpenAI Whisper API
        /// POST /api/stories/transcribe
        /// </summary>
        [HttpPost("transcribe")]
        [Authorize]
        public async Task<ActionResult<TranscriptionResponseDto>> TranscribeAudio([FromBody] TranscribeAudioRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdClaim();
            if (userId == null)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid or expired token" });
            }

            try
            {
                // Verify audio file exists in S3
                var audioExists = await _s3Service.FileExistsAsync(request.AudioS3Key);
                if (!audioExists)
                {
                    return NotFound(new { error = "NotFound", message = "Audio file not found in S3" });
                }

                // Verify the S3 key belongs to the user (basic security check)
                if (!request.AudioS3Key.Contains(userId.Value.ToString()))
                {
                    _logger.LogWarning("User {UserId} attempted to transcribe audio that doesn't belong to them: {S3Key}",
                        userId, request.AudioS3Key);
                    return StatusCode(403, new { error = "Forbidden", message = "You do not have permission to transcribe this audio" });
                }

                // Transcribe the audio (with language hint if provided)
                var response = await _whisperService.TranscribeAudioAsync(request.AudioS3Key, request.Language);

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
            {
                _logger.LogError(ex, "OpenAI API key not configured");
                return StatusCode(503, new
                {
                    error = "ServiceUnavailable",
                    message = "Transcription service temporarily unavailable"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Whisper API request failed for user {UserId}", userId);
                return StatusCode(503, new
                {
                    error = "ServiceUnavailable",
                    message = "Transcription service temporarily unavailable"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transcribe audio for user {UserId}, S3Key {S3Key}", userId, request.AudioS3Key);
                return StatusCode(500, new
                {
                    error = "InternalError",
                    message = "Failed to transcribe audio"
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<StoryReadDto>> Post([FromBody] StoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Note: Audio can be combined with image and/or video
            // No exclusive validation needed for media types anymore

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Verify bird exists using raw SQL
            var birdCheckSql = "SELECT bird_id FROM birds WHERE bird_id = @BirdId";
            var foundBirdId = await connection.QueryFirstOrDefaultAsync<Guid?>(birdCheckSql, new { BirdId = dto.BirdId });

            if (foundBirdId == null)
            {
                return NotFound(new { message = "Bird not found", birdId = dto.BirdId });
            }

            // Verify image exists in S3 if provided
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key))
            {
                var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                if (!imageExists)
                {
                    return BadRequest(new { message = "Story image not found in S3. Please upload the file first." });
                }
            }

            // Verify video exists in S3 if provided
            if (!string.IsNullOrWhiteSpace(dto.VideoS3Key))
            {
                var videoExists = await _s3Service.FileExistsAsync(dto.VideoS3Key);
                if (!videoExists)
                {
                    return BadRequest(new { message = "Story video not found in S3. Please upload the file first." });
                }
            }

            // Verify audio exists in S3 if provided
            if (!string.IsNullOrWhiteSpace(dto.AudioS3Key))
            {
                var audioExists = await _s3Service.FileExistsAsync(dto.AudioS3Key);
                if (!audioExists)
                {
                    return BadRequest(new { message = "Story audio not found in S3. Please upload the file first." });
                }
            }

            // Content moderation check
            var moderationResult = await _moderationService.ModerateStoryContentAsync(
                dto.Content,
                dto.ImageS3Key,
                dto.VideoS3Key);

            // DEBUG: Log moderation results
            _logger.LogWarning("MODERATION DEBUG: IsBlocked={IsBlocked}, ImageLabels={ImageLabels}, TextFlagged={TextFlagged}",
                moderationResult.IsBlocked,
                moderationResult.ImageResult?.DetectedLabels?.Count ?? 0,
                moderationResult.TextResult?.IsFlagged ?? false);

            if (moderationResult.IsBlocked)
            {
                _logger.LogWarning("Story content blocked by moderation for user {UserId}. Reason: {Reason}",
                    userId.Value, moderationResult.BlockReason);
                return BadRequest(new
                {
                    message = "Content blocked by moderation",
                    reason = moderationResult.BlockReason,
                    code = "CONTENT_MODERATION_BLOCKED",
                    debug = new
                    {
                        imageLabels = moderationResult.ImageResult?.DetectedLabels,
                        textCategories = moderationResult.TextResult?.FlaggedCategories
                    }
                });
            }

            var storyId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Detect language from content
            string? detectedLanguage = null;
            try
            {
                detectedLanguage = _languageDetectionService.DetectLanguage(dto.Content);
                if (detectedLanguage != null)
                {
                    _logger.LogInformation("Detected language '{Language}' for story {StoryId}", detectedLanguage, storyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect language for story {StoryId}", storyId);
            }

            // Get author's country from their profile
            string? authorCountry = null;
            try
            {
                var userCountrySql = "SELECT country FROM users WHERE user_id = @UserId";
                authorCountry = await connection.QueryFirstOrDefaultAsync<string?>(userCountrySql, new { UserId = userId.Value });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get author country for story {StoryId}", storyId);
            }

            // Insert story using raw SQL with bird_id foreign key
            var insertStorySql = @"
                INSERT INTO stories (story_id, author_id, bird_id, content, mode, image_url, video_url, audio_url, is_highlighted, created_at, language, country)
                VALUES (@StoryId, @AuthorId, @BirdId, @Content, @Mode, @ImageUrl, @VideoUrl, @AudioUrl, @IsHighlighted, @CreatedAt, @Language, @Country)";

            await connection.ExecuteAsync(insertStorySql, new
            {
                StoryId = storyId,
                AuthorId = userId.Value,
                BirdId = dto.BirdId,
                Content = dto.Content,
                Mode = dto.Mode,
                ImageUrl = dto.ImageS3Key,
                VideoUrl = dto.VideoS3Key,
                AudioUrl = dto.AudioS3Key,
                IsHighlighted = false, // Regular stories are not highlighted by default
                CreatedAt = now,
                Language = detectedLanguage,
                Country = authorCountry
            });

            _logger.LogInformation("Story created: {StoryId} by user {UserId} for bird {BirdId}",
                storyId, userId.Value, dto.BirdId);

            // Update bird's last activity timestamp
            await _activityService.UpdateLastActivityAsync(dto.BirdId);

            // Notify users who loved this bird about new story
            _ = Task.Run(async () =>
            {
                try
                {
                    // Get bird name
                    var birdNameSql = "SELECT name FROM birds WHERE bird_id = @BirdId";
                    var birdName = await connection.QueryFirstOrDefaultAsync<string>(birdNameSql, new { BirdId = dto.BirdId });

                    if (!string.IsNullOrEmpty(birdName))
                    {
                        // Use raw SQL to get lover user IDs
                        var loversSql = @"
                            SELECT DISTINCT user_id 
                            FROM loves 
                            WHERE bird_id = @BirdId AND user_id != @AuthorId";

                        var lovers = await connection.QueryAsync<Guid>(loversSql, new
                        {
                            BirdId = dto.BirdId,
                            AuthorId = userId.Value
                        });

                        foreach (var loverId in lovers)
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = loverId,
                                Type = NotificationType.NewStory,
                                Title = "New story: " + birdName,
                                Message = $"{birdName} has a new story to share!",
                                Priority = NotificationPriority.Low,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/story/{storyId}",
                                StoryId = storyId,
                                ActorUserId = userId.Value
                            });
                        }
                    }
                }
                catch
                {
                    // Ignore notification errors
                }
            });

            // Fetch created story to return
            return await Get(storyId);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(Guid id, [FromBody] StoryUpdateDto dto)
        {
            _logger.LogInformation("Edit story request for {StoryId}", id);
            _logger.LogInformation("DTO Content: {HasContent}, Length: {Length}",
                !string.IsNullOrWhiteSpace(dto.Content),
                dto.Content?.Length ?? 0);
            _logger.LogInformation("DTO ImageS3Key: {ImageS3Key}", dto.ImageS3Key ?? "NULL");
            _logger.LogInformation("DTO VideoS3Key: {VideoS3Key}", dto.VideoS3Key ?? "NULL");
            _logger.LogInformation("DTO Mode: {Mode}", dto.Mode?.ToString() ?? "NULL");
            _logger.LogInformation("DTO BirdId: {BirdId}", dto.BirdId?.ToString() ?? "NULL");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for story update: {StoryId}", id);
                return BadRequest(ModelState);
            }

            var userId = GetUserIdClaim();
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized story update attempt for story: {StoryId}", id);
                return Unauthorized();
            }

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get story with raw SQL
            var getStorySql = @"
                SELECT story_id, author_id, bird_id, content, mode, image_url, video_url, audio_url, created_at, is_highlighted, highlight_order
                FROM stories
                WHERE story_id = @StoryId";

            var story = await connection.QueryFirstOrDefaultAsync<dynamic>(getStorySql, new { StoryId = id });

            if (story == null)
            {
                _logger.LogWarning("Story not found: {StoryId}", id);
                return NotFound(new { message = "Story not found" });
            }

            // Only author can update story
            if ((Guid)story.author_id != userId.Value)
            {
                _logger.LogWarning("User {UserId} attempted to update story {StoryId} owned by {OwnerId}",
                    userId.Value, id, (Guid)story.author_id);
                return Forbid();
            }

            _logger.LogInformation("Current story content length: {Length}", ((string)story.content).Length);

            var updates = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("StoryId", id);

            // Update content if provided
            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                _logger.LogInformation("Updating story content from {OldLength} to {NewLength} chars",
                    ((string)story.content).Length, dto.Content.Length);
                updates.Add("content = @Content");
                parameters.Add("Content", dto.Content.Trim());
            }
            else if (dto.Content != null)
            {
                _logger.LogWarning("Empty content provided for story {StoryId} - content is required", id);
                return BadRequest(new { message = "Story content cannot be empty" });
            }

            // Update mode if provided
            if (dto.Mode.HasValue)
            {
                _logger.LogInformation("Updating story mode from {OldMode} to {NewMode}",
                    (int?)story.mode, (int)dto.Mode.Value);
                updates.Add("mode = @Mode");
                parameters.Add("Mode", dto.Mode.Value);
            }

            // Update bird if provided
            if (dto.BirdId.HasValue)
            {
                // Validate that bird exists
                var birdCheckSql = "SELECT bird_id FROM birds WHERE bird_id = @BirdId";
                var foundBirdId = await connection.QueryFirstOrDefaultAsync<Guid?>(birdCheckSql, new { BirdId = dto.BirdId.Value });

                if (foundBirdId == null)
                {
                    return NotFound(new { message = "Bird not found", birdId = dto.BirdId.Value });
                }

                updates.Add("bird_id = @BirdId");
                parameters.Add("BirdId", dto.BirdId.Value);

                _logger.LogInformation("Updated bird for story {StoryId} to bird {BirdId}", id, dto.BirdId.Value);
            }

            // Update image if provided
            if (dto.ImageS3Key != null) // Check for null to distinguish between not provided and empty string
            {
                if (string.IsNullOrWhiteSpace(dto.ImageS3Key))
                {
                    _logger.LogInformation("Removing image from story {StoryId}", id);
                    // Remove image
                    string? oldImageUrl = story.image_url;
                    if (!string.IsNullOrWhiteSpace(oldImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldImageUrl);
                            _logger.LogInformation("Deleted story image for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story image");
                        }
                    }
                    updates.Add("image_url = NULL");
                }
                else if (dto.ImageS3Key != (string?)story.image_url)
                {
                    string? oldImageUrl = story.image_url;
                    _logger.LogInformation("Updating image for story {StoryId} from {OldKey} to {NewKey}",
                        id, oldImageUrl ?? "NULL", dto.ImageS3Key);
                    // New image provided
                    var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                    if (!imageExists)
                    {
                        _logger.LogWarning("Image S3 key not found: {ImageS3Key}", dto.ImageS3Key);
                        return BadRequest(new { message = "Story image not found in S3. Please upload the file first." });
                    }

                    // Delete old image
                    if (!string.IsNullOrWhiteSpace(oldImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldImageUrl);
                            _logger.LogInformation("Deleted old story image");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story image");
                        }
                    }

                    updates.Add("image_url = @ImageUrl");
                    parameters.Add("ImageUrl", dto.ImageS3Key);

                    // IMPORTANT: Setting image removes video (one media type only)
                    string? oldVideoUrl = story.video_url;
                    if (!string.IsNullOrWhiteSpace(oldVideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldVideoUrl);
                            _logger.LogInformation("Deleted story video when switching to image for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story video when switching to image");
                        }
                        updates.Add("video_url = NULL");
                    }
                }
            }

            // Update video if provided
            if (dto.VideoS3Key != null) // Check for null to distinguish between not provided and empty string
            {
                if (string.IsNullOrWhiteSpace(dto.VideoS3Key))
                {
                    _logger.LogInformation("Removing video from story {StoryId}", id);
                    // Remove video
                    string? oldVideoUrl = story.video_url;
                    if (!string.IsNullOrWhiteSpace(oldVideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldVideoUrl);
                            _logger.LogInformation("Deleted story video for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story video");
                        }
                    }
                    updates.Add("video_url = NULL");
                }
                else if (dto.VideoS3Key != (string?)story.video_url)
                {
                    string? oldVideoUrl = story.video_url;
                    _logger.LogInformation("Updating video for story {StoryId} from {OldKey} to {NewKey}",
                        id, oldVideoUrl ?? "NULL", dto.VideoS3Key);
                    // New video provided
                    var videoExists = await _s3Service.FileExistsAsync(dto.VideoS3Key);
                    if (!videoExists)
                    {
                        _logger.LogWarning("Video S3 key not found: {VideoS3Key}", dto.VideoS3Key);
                        return BadRequest(new { message = "Story video not found in S3. Please upload the file first." });
                    }

                    // Delete old video
                    if (!string.IsNullOrWhiteSpace(oldVideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldVideoUrl);
                            _logger.LogInformation("Deleted old story video");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story video");
                        }
                    }

                    updates.Add("video_url = @VideoUrl");
                    parameters.Add("VideoUrl", dto.VideoS3Key);

                    // IMPORTANT: Setting video removes image (one media type only)
                    string? oldImageUrl = story.image_url;
                    if (!string.IsNullOrWhiteSpace(oldImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldImageUrl);
                            _logger.LogInformation("Deleted story image when switching to video for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story image when switching to video");
                        }
                        updates.Add("image_url = NULL");
                    }
                }
            }

            // Update audio if provided (audio can coexist with image/video)
            if (dto.AudioS3Key != null) // Check for null to distinguish between not provided and empty string
            {
                if (string.IsNullOrWhiteSpace(dto.AudioS3Key))
                {
                    _logger.LogInformation("Removing audio from story {StoryId}", id);
                    // Remove audio
                    string? oldAudioUrl = story.audio_url;
                    if (!string.IsNullOrWhiteSpace(oldAudioUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldAudioUrl);
                            _logger.LogInformation("Deleted story audio for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story audio");
                        }
                    }
                    updates.Add("audio_url = NULL");
                }
                else if (dto.AudioS3Key != (string?)story.audio_url)
                {
                    string? oldAudioUrl = story.audio_url;
                    _logger.LogInformation("Updating audio for story {StoryId} from {OldKey} to {NewKey}",
                        id, oldAudioUrl ?? "NULL", dto.AudioS3Key);
                    // New audio provided
                    var audioExists = await _s3Service.FileExistsAsync(dto.AudioS3Key);
                    if (!audioExists)
                    {
                        _logger.LogWarning("Audio S3 key not found: {AudioS3Key}", dto.AudioS3Key);
                        return BadRequest(new { message = "Story audio not found in S3. Please upload the file first." });
                    }

                    // Delete old audio
                    if (!string.IsNullOrWhiteSpace(oldAudioUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(oldAudioUrl);
                            _logger.LogInformation("Deleted old story audio");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story audio");
                        }
                    }

                    updates.Add("audio_url = @AudioUrl");
                    parameters.Add("AudioUrl", dto.AudioS3Key);
                    // Note: Audio can coexist with image/video, so no cross-deletion needed
                }
            }

            // Content moderation check for any updated content
            if (!string.IsNullOrWhiteSpace(dto.Content) ||
                (!string.IsNullOrWhiteSpace(dto.ImageS3Key) && dto.ImageS3Key != (string?)story.image_url) ||
                (!string.IsNullOrWhiteSpace(dto.VideoS3Key) && dto.VideoS3Key != (string?)story.video_url))
            {
                var moderationResult = await _moderationService.ModerateStoryContentAsync(
                    dto.Content,
                    !string.IsNullOrWhiteSpace(dto.ImageS3Key) && dto.ImageS3Key != (string?)story.image_url ? dto.ImageS3Key : null,
                    !string.IsNullOrWhiteSpace(dto.VideoS3Key) && dto.VideoS3Key != (string?)story.video_url ? dto.VideoS3Key : null);

                if (moderationResult.IsBlocked)
                {
                    _logger.LogWarning("Story update content blocked by moderation for story {StoryId}. Reason: {Reason}",
                        id, moderationResult.BlockReason);
                    return BadRequest(new
                    {
                        message = "Content blocked by moderation",
                        reason = moderationResult.BlockReason,
                        code = "CONTENT_MODERATION_BLOCKED"
                    });
                }
            }

            // Execute update if there are changes
            if (updates.Any())
            {
                var updateSql = $"UPDATE stories SET {string.Join(", ", updates)} WHERE story_id = @StoryId";
                await connection.ExecuteAsync(updateSql, parameters);

                _logger.LogInformation("Story updated successfully: {StoryId}", id);
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get story using raw SQL
            var getStorySql = "SELECT author_id, image_url, video_url, audio_url FROM stories WHERE story_id = @StoryId";
            var story = await connection.QueryFirstOrDefaultAsync<dynamic>(getStorySql, new { StoryId = id });

            if (story == null) return NotFound();

            // Only author can delete story
            if ((Guid)story.author_id != userId.Value) return Forbid();

            // Delete image from S3 if exists
            if (!string.IsNullOrWhiteSpace(story.image_url))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(story.image_url);
                    _logger.LogInformation("Deleted story image for story {StoryId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete story image from S3");
                }
            }

            // Delete video from S3 if exists
            if (!string.IsNullOrWhiteSpace(story.video_url))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(story.video_url);
                    _logger.LogInformation("Deleted story video for story {StoryId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete story video from S3");
                }
            }

            // Delete audio from S3 if exists
            if (!string.IsNullOrWhiteSpace(story.audio_url))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(story.audio_url);
                    _logger.LogInformation("Deleted story audio for story {StoryId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete story audio from S3");
                }
            }

            // Delete story using raw SQL (cascade will delete story_birds)
            await connection.ExecuteAsync("DELETE FROM stories WHERE story_id = @StoryId", new { StoryId = id });

            _logger.LogInformation("Story deleted: {StoryId}", id);

            return NoContent();
        }

        [Authorize]
        [HttpPatch("{id}/highlight")]
        public async Task<IActionResult> ToggleHighlight(Guid id, [FromBody] StoryHighlightDto dto)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get story with bird info using raw SQL - direct relationship
            var getStoryBirdSql = @"
                SELECT s.story_id, s.bird_id, s.is_highlighted, s.highlight_order, b.owner_id
                FROM stories s
                LEFT JOIN birds b ON s.bird_id = b.bird_id
                WHERE s.story_id = @StoryId";

            var story = await connection.QueryFirstOrDefaultAsync<dynamic>(getStoryBirdSql, new { StoryId = id });

            if (story == null) return NotFound();

            // Verify story has a bird
            if (story.bird_id == null)
            {
                return BadRequest("Story must have a bird");
            }

            Guid birdId = (Guid)story.bird_id;

            // Verify user owns the bird
            if (story.owner_id == null || (Guid)story.owner_id != userId.Value)
            {
                return Forbid("You must own the bird in this story");
            }

            // Check if bird is premium
            var premiumCheckSql = @"
                SELECT COUNT(*) 
                FROM bird_premium_subscriptions 
                WHERE bird_id = @BirdId AND status = 'active'";
            var hasPremiumBird = await connection.ExecuteScalarAsync<int>(premiumCheckSql, new { BirdId = birdId }) > 0;

            if (!hasPremiumBird)
            {
                return Forbid("The bird in this story must be premium");
            }

            // When highlighting, enforce a max of 3 highlights per bird
            if (dto.IsHighlighted)
            {
                var countSql = @"
                    SELECT COUNT(*)
                    FROM stories
                    WHERE bird_id = @BirdId AND is_highlighted = true";
                var count = await connection.ExecuteScalarAsync<int>(countSql, new { BirdId = birdId });

                if (count >= 3)
                {
                    return BadRequest($"Maximum of 3 highlights allowed per bird");
                }

                // If pin requested, set highlight order to 1 and bump others
                if (dto.PinToProfile)
                {
                    var bumpSql = @"
                        UPDATE stories
                        SET highlight_order = COALESCE(highlight_order, 1) + 1
                        WHERE bird_id = @BirdId 
                          AND is_highlighted = true";
                    await connection.ExecuteAsync(bumpSql, new { BirdId = birdId });

                    await connection.ExecuteAsync(
                        "UPDATE stories SET is_highlighted = true, highlight_order = 1 WHERE story_id = @StoryId",
                        new { StoryId = id });
                }
                else
                {
                    // set next order - get max for this bird
                    var ordersSql = @"
                        SELECT highlight_order
                        FROM stories
                        WHERE bird_id = @BirdId 
                          AND is_highlighted = true 
                          AND highlight_order IS NOT NULL";

                    var highlightOrders = await connection.QueryAsync<int>(ordersSql, new { BirdId = birdId });

                    int maxOrder = highlightOrders.Any() ? highlightOrders.Max() : 0;

                    await connection.ExecuteAsync(
                        "UPDATE stories SET is_highlighted = true, highlight_order = @Order WHERE story_id = @StoryId",
                        new { Order = maxOrder + 1, StoryId = id });
                }
            }
            else
            {
                // Clearing highlight, compact orders for this bird
                if (story.is_highlighted && story.highlight_order != null)
                {
                    var currentOrder = (int)story.highlight_order;

                    var compactSql = @"
                        UPDATE stories
                        SET highlight_order = highlight_order - 1
                        WHERE bird_id = @BirdId 
                          AND is_highlighted = true 
                          AND highlight_order > @CurrentOrder";
                    await connection.ExecuteAsync(compactSql, new { BirdId = birdId, CurrentOrder = currentOrder });
                }

                await connection.ExecuteAsync(
                    "UPDATE stories SET is_highlighted = false, highlight_order = NULL WHERE story_id = @StoryId",
                    new { StoryId = id });
            }

            return Ok();
        }
    }
}
