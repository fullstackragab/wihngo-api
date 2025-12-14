namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
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
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IS3Service _s3Service;
        private readonly ILogger<StoriesController> _logger;

        public StoriesController(
            AppDbContext db, 
            IMapper mapper, 
            INotificationService notificationService,
            IS3Service s3Service,
            ILogger<StoriesController> logger)
        {
            _db = db;
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

        [HttpGet]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var total = await _db.Stories.CountAsync();

            // Split query to avoid PostgreSQL ordered-set aggregate issues
            // First get the story IDs with ordering and pagination
            var storyIds = await _db.Stories
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.StoryId)
                .ToListAsync();

            // Then fetch the full stories with includes using Join instead of Contains
            var items = await (from story in _db.Stories
                               join id in storyIds on story.StoryId equals id
                               select story)
                .Include(s => s.StoryBirds)
                    .ThenInclude(sb => sb.Bird)
                .Include(s => s.Author)
                .ToListAsync();

            // Re-apply ordering in memory (since SQL WHERE IN doesn't preserve order)
            items = items.OrderByDescending(s => s.CreatedAt).ToList();

            var dtoItems = new List<StorySummaryDto>();
            foreach (var story in items)
            {
                var dto = new StorySummaryDto
                {
                    StoryId = story.StoryId,
                    Birds = story.StoryBirds
                        .OrderBy(sb => sb.CreatedAt)
                        .Where(sb => sb.Bird != null)
                        .Select(sb => sb.Bird!.Name)
                        .ToList(),
                    Mode = story.Mode,
                    Date = story.CreatedAt.ToString("MMMM d, yyyy"),
                    Preview = story.Content.Length > 140 ? story.Content.Substring(0, 140) + "..." : story.Content,
                    ImageS3Key = story.ImageUrl,
                    VideoS3Key = story.VideoUrl
                };

                // Generate download URL for image if it exists
                if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                {
                    try
                    {
                        dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(story.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate download URL for story image {StoryId}", story.StoryId);
                    }
                }

                // Generate download URL for video if it exists
                if (!string.IsNullOrWhiteSpace(story.VideoUrl))
                {
                    try
                    {
                        dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(story.VideoUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", story.StoryId);
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

            var total = await _db.Stories.Where(s => s.AuthorId == userId).CountAsync();

            // Split query to avoid PostgreSQL ordered-set aggregate issues
            // First get the story IDs with ordering and pagination
            var storyIds = await _db.Stories
                .Where(s => s.AuthorId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.StoryId)
                .ToListAsync();

            // Then fetch the full stories with includes using Join instead of Contains
            var items = await (from story in _db.Stories
                               join id in storyIds on story.StoryId equals id
                               select story)
                .Include(s => s.StoryBirds)
                    .ThenInclude(sb => sb.Bird)
                .Include(s => s.Author)
                .ToListAsync();

            // Re-apply ordering in memory (since SQL WHERE IN doesn't preserve order)
            items = items.OrderByDescending(s => s.CreatedAt).ToList();

            var dtoItems = new List<StorySummaryDto>();
            foreach (var story in items)
            {
                var dto = new StorySummaryDto
                {
                    StoryId = story.StoryId,
                    Birds = story.StoryBirds
                        .OrderBy(sb => sb.CreatedAt)
                        .Where(sb => sb.Bird != null)
                        .Select(sb => sb.Bird!.Name)
                        .ToList(),
                    Mode = story.Mode,
                    Date = story.CreatedAt.ToString("MMMM d, yyyy"),
                    Preview = story.Content.Length > 140 ? story.Content.Substring(0, 140) + "..." : story.Content,
                    ImageS3Key = story.ImageUrl,
                    VideoS3Key = story.VideoUrl
                };

                // Generate download URL if image exists
                if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                {
                    try
                    {
                        dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(story.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate download URL for story {StoryId}", story.StoryId);
                    }
                }

                // Generate download URL if video exists
                if (!string.IsNullOrWhiteSpace(story.VideoUrl))
                {
                    try
                    {
                        dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(story.VideoUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", story.StoryId);
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
            var story = await _db.Stories
                .Include(s => s.StoryBirds)
                    .ThenInclude(sb => sb.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null) return NotFound();
            
            var dto = new StoryReadDto
            {
                StoryId = story.StoryId,
                Content = story.Content,
                Mode = story.Mode,
                ImageS3Key = story.ImageUrl,
                VideoS3Key = story.VideoUrl,
                CreatedAt = story.CreatedAt,
                Author = story.Author != null ? new UserSummaryDto
                {
                    UserId = story.Author.UserId,
                    Name = story.Author.Name
                } : new UserSummaryDto(),
                Birds = story.StoryBirds
                    .OrderBy(sb => sb.CreatedAt)
                    .Where(sb => sb.Bird != null)
                    .Select(sb => new BirdSummaryDto
                    {
                        BirdId = sb.Bird!.BirdId,
                        Name = sb.Bird.Name,
                        Species = sb.Bird.Species,
                        ImageS3Key = sb.Bird.ImageUrl,
                        VideoS3Key = sb.Bird.VideoUrl,
                        Tagline = sb.Bird.Tagline,
                        LovedBy = sb.Bird.LovedCount,
                        SupportedBy = sb.Bird.SupportedCount,
                        OwnerId = sb.Bird.OwnerId
                    })
                    .ToList()
            };
            
            // Generate download URL for story image
            if (!string.IsNullOrWhiteSpace(story.ImageUrl))
            {
                try
                {
                    dto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(story.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for story {StoryId}", id);
                }
            }

            // Generate download URL for story video
            if (!string.IsNullOrWhiteSpace(story.VideoUrl))
            {
                try
                {
                    dto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(story.VideoUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate video download URL for story {StoryId}", id);
                }
            }

            // Generate download URLs for bird images and videos
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
                if (!string.IsNullOrWhiteSpace(bird.VideoS3Key))
                {
                    try
                    {
                        bird.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(bird.VideoS3Key);
                    }
                    catch { }
                }
            }
            
            return Ok(dto);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<StoryReadDto>> Post([FromBody] StoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Validate that at least one bird is selected
            if (dto.BirdIds == null || dto.BirdIds.Count == 0)
            {
                return BadRequest(new { message = "At least one bird must be selected" });
            }

            // Validate that only ONE media type is provided (image OR video, not both)
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key) && !string.IsNullOrWhiteSpace(dto.VideoS3Key))
            {
                return BadRequest(new { message = "Story can have either an image or a video, not both" });
            }

            // Verify all birds exist
            var birds = await _db.Birds.Where(b => dto.BirdIds.Contains(b.BirdId)).ToListAsync();
            if (birds.Count != dto.BirdIds.Count)
            {
                var foundIds = birds.Select(b => b.BirdId).ToList();
                var missingIds = dto.BirdIds.Except(foundIds).ToList();
                return NotFound(new { message = "Some birds not found", missingBirdIds = missingIds });
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

            var story = new Story
            {
                StoryId = Guid.NewGuid(),
                AuthorId = userId.Value,
                Content = dto.Content,
                Mode = dto.Mode, // Optional - can be null
                ImageUrl = dto.ImageS3Key,
                VideoUrl = dto.VideoS3Key,
                CreatedAt = DateTime.UtcNow
            };

            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            // Create story-bird relationships
            foreach (var birdId in dto.BirdIds)
            {
                var storyBird = new StoryBird
                {
                    StoryBirdId = Guid.NewGuid(),
                    StoryId = story.StoryId,
                    BirdId = birdId,
                    CreatedAt = DateTime.UtcNow
                };
                _db.StoryBirds.Add(storyBird);
            }
            await _db.SaveChangesAsync();

            _logger.LogInformation("Story created: {StoryId} by user {UserId} with {BirdCount} birds", 
                story.StoryId, userId.Value, dto.BirdIds.Count);

            // Load created story with relationships
            var created = await _db.Stories
                .Include(s => s.StoryBirds)
                    .ThenInclude(sb => sb.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == story.StoryId);

            // Notify users who loved these birds about new story
            if (created?.StoryBirds != null && created.StoryBirds.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var birdIds = created.StoryBirds.Select(sb => sb.BirdId).ToList();
                        var birdNames = created.StoryBirds
                            .Where(sb => sb.Bird != null)
                            .Select(sb => sb.Bird!.Name)
                            .ToList();
                        var birdNamesStr = string.Join(", ", birdNames);

                        var lovers = await _db.Loves
                            .Where(l => birdIds.Contains(l.BirdId) && l.UserId != story.AuthorId)
                            .Select(l => l.UserId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var loverId in lovers)
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = loverId,
                                Type = NotificationType.NewStory,
                                Title = "Book New story: " + birdNamesStr,
                                Message = $"{birdNamesStr} has a new story to share!",
                                Priority = NotificationPriority.Low,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/story/{story.StoryId}",
                                StoryId = story.StoryId,
                                ActorUserId = story.AuthorId
                            });
                        }
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            var resultDto = new StoryReadDto
            {
                StoryId = created!.StoryId,
                Content = created.Content,
                Mode = created.Mode,
                ImageS3Key = created.ImageUrl,
                VideoS3Key = created.VideoUrl,
                CreatedAt = created.CreatedAt,
                Author = created.Author != null ? new UserSummaryDto
                {
                    UserId = created.Author.UserId,
                    Name = created.Author.Name
                } : new UserSummaryDto(),
                Birds = created.StoryBirds
                    .OrderBy(sb => sb.CreatedAt)
                    .Where(sb => sb.Bird != null)
                    .Select(sb => new BirdSummaryDto
                    {
                        BirdId = sb.Bird!.BirdId,
                        Name = sb.Bird.Name,
                        Species = sb.Bird.Species,
                        ImageS3Key = sb.Bird.ImageUrl,
                        VideoS3Key = sb.Bird.VideoUrl,
                        Tagline = sb.Bird.Tagline,
                        LovedBy = sb.Bird.LovedCount,
                        SupportedBy = sb.Bird.SupportedCount,
                        OwnerId = sb.Bird.OwnerId
                    })
                    .ToList()
            };
            
            // Generate download URLs
            if (!string.IsNullOrWhiteSpace(created.ImageUrl))
            {
                try
                {
                    resultDto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(created.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for story image {StoryId}", story.StoryId);
                }
            }

            if (!string.IsNullOrWhiteSpace(created.VideoUrl))
            {
                try
                {
                    resultDto.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(created.VideoUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for story video {StoryId}", story.StoryId);
                }
            }

            // Generate download URLs for bird images
            foreach (var bird in resultDto.Birds)
            {
                if (!string.IsNullOrWhiteSpace(bird.ImageS3Key))
                {
                    try
                    {
                        bird.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(bird.ImageS3Key);
                    }
                    catch { }
                }
                if (!string.IsNullOrWhiteSpace(bird.VideoS3Key))
                {
                    try
                    {
                        bird.VideoUrl = await _s3Service.GenerateDownloadUrlAsync(bird.VideoS3Key);
                    }
                    catch { }
                }
            }

            return CreatedAtAction(nameof(Get), new { id = story.StoryId }, resultDto);
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
            _logger.LogInformation("DTO BirdIds: {BirdCount}", dto.BirdIds?.Count ?? 0);

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

            var story = await _db.Stories
                .Include(s => s.StoryBirds)
                .FirstOrDefaultAsync(s => s.StoryId == id);
                
            if (story == null)
            {
                _logger.LogWarning("Story not found: {StoryId}", id);
                return NotFound(new { message = "Story not found" });
            }

            // Only author can update story
            if (story.AuthorId != userId.Value)
            {
                _logger.LogWarning("User {UserId} attempted to update story {StoryId} owned by {OwnerId}", 
                    userId.Value, id, story.AuthorId);
                return Forbid();
            }

            _logger.LogInformation("Current story content length: {Length}", story.Content.Length);

            // Update content if provided
            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                _logger.LogInformation("Updating story content from {OldLength} to {NewLength} chars", 
                    story.Content.Length, dto.Content.Length);
                story.Content = dto.Content.Trim();
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
                    story.Mode, dto.Mode.Value);
                story.Mode = dto.Mode.Value;
            }

            // Update birds if provided
            if (dto.BirdIds != null && dto.BirdIds.Any())
            {
                // Validate that all birds exist
                var birds = await _db.Birds.Where(b => dto.BirdIds.Contains(b.BirdId)).ToListAsync();
                if (birds.Count != dto.BirdIds.Count)
                {
                    var foundIds = birds.Select(b => b.BirdId).ToList();
                    var missingIds = dto.BirdIds.Except(foundIds).ToList();
                    return NotFound(new { message = "Some birds not found", missingBirdIds = missingIds });
                }

                // Remove existing bird associations
                var existingStoryBirds = await _db.StoryBirds.Where(sb => sb.StoryId == id).ToListAsync();
                _db.StoryBirds.RemoveRange(existingStoryBirds);

                // Add new bird associations
                foreach (var birdId in dto.BirdIds)
                {
                    var storyBird = new StoryBird
                    {
                        StoryBirdId = Guid.NewGuid(),
                        StoryId = id,
                        BirdId = birdId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.StoryBirds.Add(storyBird);
                }
                
                _logger.LogInformation("Updated birds for story {StoryId}, new count: {Count}", id, dto.BirdIds.Count);
            }

            // Update image if provided
            if (dto.ImageS3Key != null) // Check for null to distinguish between not provided and empty string
            {
                if (string.IsNullOrWhiteSpace(dto.ImageS3Key))
                {
                    _logger.LogInformation("Removing image from story {StoryId}", id);
                    // Remove image
                    if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.ImageUrl);
                            _logger.LogInformation("Deleted story image for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story image");
                        }
                    }
                    story.ImageUrl = null;
                }
                else if (dto.ImageS3Key != story.ImageUrl)
                {
                    _logger.LogInformation("Updating image for story {StoryId} from {OldKey} to {NewKey}", 
                        id, story.ImageUrl ?? "NULL", dto.ImageS3Key);
                    // New image provided
                    var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                    if (!imageExists)
                    {
                        _logger.LogWarning("Image S3 key not found: {ImageS3Key}", dto.ImageS3Key);
                        return BadRequest(new { message = "Story image not found in S3. Please upload the file first." });
                    }

                    // Delete old image
                    if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.ImageUrl);
                            _logger.LogInformation("Deleted old story image");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story image");
                        }
                    }

                    story.ImageUrl = dto.ImageS3Key;
                    
                    // IMPORTANT: Setting image removes video (one media type only)
                    if (!string.IsNullOrWhiteSpace(story.VideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.VideoUrl);
                            _logger.LogInformation("Deleted story video when switching to image for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story video when switching to image");
                        }
                        story.VideoUrl = null;
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
                    if (!string.IsNullOrWhiteSpace(story.VideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.VideoUrl);
                            _logger.LogInformation("Deleted story video for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story video");
                        }
                    }
                    story.VideoUrl = null;
                }
                else if (dto.VideoS3Key != story.VideoUrl)
                {
                    _logger.LogInformation("Updating video for story {StoryId} from {OldKey} to {NewKey}", 
                        id, story.VideoUrl ?? "NULL", dto.VideoS3Key);
                    // New video provided
                    var videoExists = await _s3Service.FileExistsAsync(dto.VideoS3Key);
                    if (!videoExists)
                    {
                        _logger.LogWarning("Video S3 key not found: {VideoS3Key}", dto.VideoS3Key);
                        return BadRequest(new { message = "Story video not found in S3. Please upload the file first." });
                    }

                    // Delete old video
                    if (!string.IsNullOrWhiteSpace(story.VideoUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.VideoUrl);
                            _logger.LogInformation("Deleted old story video");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story video");
                        }
                    }

                    story.VideoUrl = dto.VideoS3Key;
                    
                    // IMPORTANT: Setting video removes image (one media type only)
                    if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.ImageUrl);
                            _logger.LogInformation("Deleted story image when switching to video for story {StoryId}", id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete story image when switching to video");
                        }
                        story.ImageUrl = null;
                    }
                }
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Story updated successfully: {StoryId}, Content length: {Length}", 
                id, story.Content.Length);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            // Only author can delete story
            if (story.AuthorId != userId.Value) return Forbid();

            // Delete image from S3 if exists
            if (!string.IsNullOrWhiteSpace(story.ImageUrl))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(story.ImageUrl);
                    _logger.LogInformation("Deleted story image for story {StoryId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete story image from S3");
                }
            }

            // Delete video from S3 if exists
            if (!string.IsNullOrWhiteSpace(story.VideoUrl))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(story.VideoUrl);
                    _logger.LogInformation("Deleted story video for story {StoryId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete story video from S3");
                }
            }

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Story deleted: {StoryId}", id);
            
            return NoContent();
        }

        [Authorize]
        [HttpPatch("{id}/highlight")]
        public async Task<IActionResult> ToggleHighlight(Guid id, [FromBody] StoryHighlightDto dto)
        {
            var story = await _db.Stories
                .Include(s => s.StoryBirds)
                    .ThenInclude(sb => sb.Bird)
                .FirstOrDefaultAsync(s => s.StoryId == id);
                
            if (story == null) return NotFound();

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Verify story has at least one bird and user owns at least one of the birds
            if (story.StoryBirds == null || !story.StoryBirds.Any())
            {
                return BadRequest("Story must have at least one bird");
            }

            var userOwnsBird = story.StoryBirds.Any(sb => sb.Bird != null && sb.Bird.OwnerId == userId.Value);
            if (!userOwnsBird)
            {
                return Forbid("You must own at least one of the birds in this story");
            }

            // Get the bird IDs from the story
            var birdIds = story.StoryBirds.Select(sb => sb.BirdId).ToList();

            // Check if any bird is premium
            var hasPremiumBird = await _db.BirdPremiumSubscriptions
                .AnyAsync(s => birdIds.Contains(s.BirdId) && s.Status == "active");
                
            if (!hasPremiumBird)
            {
                return Forbid("At least one bird in the story must be premium");
            }

            // When highlighting, enforce a max of 3 highlights per bird
            if (dto.IsHighlighted)
            {
                foreach (var birdId in birdIds)
                {
                    var count = await _db.Stories
                        .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && s.IsHighlighted)
                        .CountAsync();
                        
                    if (count >= 3)
                    {
                        return BadRequest($"Maximum of 3 highlights allowed per bird");
                    }
                }

                // If pin requested, set highlight order to 1 and bump others
                if (dto.PinToProfile)
                {
                    foreach (var birdId in birdIds)
                    {
                        var existing = await _db.Stories
                            .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && s.IsHighlighted)
                            .ToListAsync();
                            
                        foreach (var ex in existing)
                        {
                            ex.HighlightOrder = (ex.HighlightOrder ?? 1) + 1;
                        }
                    }
                    story.HighlightOrder = 1;
                }
                else
                {
                    // set next order - get max across all birds in story
                    int maxOrder = 0;
                    foreach (var birdId in birdIds)
                    {
                        var highlightOrders = await _db.Stories
                            .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && s.IsHighlighted && s.HighlightOrder != null)
                            .Select(s => s.HighlightOrder!.Value)
                            .ToListAsync();
                            
                        if (highlightOrders.Any())
                        {
                            var birdMax = highlightOrders.Max();
                            if (birdMax > maxOrder)
                            {
                                maxOrder = birdMax;
                            }
                        }
                    }
                    story.HighlightOrder = maxOrder + 1;
                }
            }
            else
            {
                // Clearing highlight, compact orders for all birds
                if (story.IsHighlighted && story.HighlightOrder != null)
                {
                    var currentOrder = story.HighlightOrder.Value;
                    foreach (var birdId in birdIds)
                    {
                        var others = await _db.Stories
                            .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && 
                                       s.IsHighlighted && 
                                       s.HighlightOrder > currentOrder)
                            .ToListAsync();
                            
                        foreach (var o in others)
                        {
                            o.HighlightOrder = o.HighlightOrder - 1;
                        }
                    }
                }
                story.HighlightOrder = null;
            }

            story.IsHighlighted = dto.IsHighlighted;

            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
