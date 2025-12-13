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

            var total = await _db.Stories.LongCountAsync();

            var items = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtoItems = new List<StorySummaryDto>();
            foreach (var story in items)
            {
                var dto = new StorySummaryDto
                {
                    StoryId = story.StoryId,
                    Title = story.Content.Length > 30 ? story.Content.Substring(0, 30) + "..." : story.Content,
                    Bird = story.Bird?.Name ?? string.Empty,
                    Date = story.CreatedAt.ToString("MMMM d, yyyy"),
                    Preview = story.Content.Length > 140 ? story.Content.Substring(0, 140) + "..." : story.Content,
                    ImageS3Key = story.ImageUrl
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

        [HttpGet("{id}")]
        public async Task<ActionResult<StoryReadDto>> Get(Guid id)
        {
            var story = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null) return NotFound();
            
            var dto = _mapper.Map<StoryReadDto>(story);
            
            // Store S3 key and generate download URL
            dto.ImageS3Key = story.ImageUrl;
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
            
            return Ok(dto);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<StoryReadDto>> Post([FromBody] StoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Verify bird exists
            var bird = await _db.Birds.FindAsync(dto.BirdId);
            if (bird == null) return NotFound(new { message = "Bird not found" });

            // Verify image exists in S3 if provided
            if (!string.IsNullOrWhiteSpace(dto.ImageS3Key))
            {
                var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                if (!imageExists)
                {
                    return BadRequest(new { message = "Story image not found in S3. Please upload the file first." });
                }
            }

            var story = new Story
            {
                StoryId = Guid.NewGuid(),
                BirdId = dto.BirdId,
                AuthorId = userId.Value,
                Content = dto.Content,
                ImageUrl = dto.ImageS3Key,
                CreatedAt = DateTime.UtcNow
            };

            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Story created: {StoryId} by user {UserId}", story.StoryId, userId.Value);

            var created = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == story.StoryId);

            // Notify users who loved this bird about new story
            if (created?.Bird != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var lovers = await _db.Loves
                            .Where(l => l.BirdId == story.BirdId && l.UserId != story.AuthorId)
                            .Select(l => l.UserId)
                            .ToListAsync();

                        foreach (var loverId in lovers)
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = loverId,
                                Type = NotificationType.NewStory,
                                Title = "Book New story: " + created.Bird.Name,
                                Message = $"{created.Bird.Name} has a new story to share!",
                                Priority = NotificationPriority.Low,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/story/{story.StoryId}",
                                BirdId = story.BirdId,
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

            var resultDto = _mapper.Map<StoryReadDto>(created);
            resultDto.ImageS3Key = created!.ImageUrl;
            
            // Generate download URL
            if (!string.IsNullOrWhiteSpace(created.ImageUrl))
            {
                try
                {
                    resultDto.ImageUrl = await _s3Service.GenerateDownloadUrlAsync(created.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for story {StoryId}", story.StoryId);
                }
            }

            return CreatedAtAction(nameof(Get), new { id = story.StoryId }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(Guid id, [FromBody] StoryUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            // Only author can update story
            if (story.AuthorId != userId.Value) return Forbid();

            // Update content if provided
            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                story.Content = dto.Content;
            }

            // Update image if provided
            if (dto.ImageS3Key != null) // Check for null to distinguish between not provided and empty string
            {
                if (string.IsNullOrWhiteSpace(dto.ImageS3Key))
                {
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
                    // New image provided
                    var imageExists = await _s3Service.FileExistsAsync(dto.ImageS3Key);
                    if (!imageExists)
                    {
                        return BadRequest(new { message = "Story image not found in S3." });
                    }

                    // Delete old image
                    if (!string.IsNullOrWhiteSpace(story.ImageUrl))
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(story.ImageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old story image");
                        }
                    }

                    story.ImageUrl = dto.ImageS3Key;
                }
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Story updated: {StoryId}", id);
            
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

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Story deleted: {StoryId}", id);
            
            return NoContent();
        }

        [Authorize]
        [HttpPatch("{id}/highlight")]
        public async Task<IActionResult> ToggleHighlight(Guid id, [FromBody] StoryHighlightDto dto)
        {
            var story = await _db.Stories.Include(s => s.Bird).FirstOrDefaultAsync(s => s.StoryId == id);
            if (story == null) return NotFound();

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            // Only bird owner can set highlights
            if (story.Bird == null || story.Bird.OwnerId != userId.Value) return Forbid();

            // Check bird premium status
            var active = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == story.BirdId && s.Status == "active");
            if (active == null) return Forbid("Bird is not premium");

            // When highlighting, enforce a max of 3 highlights
            if (dto.IsHighlighted)
            {
                var count = await _db.Stories.CountAsync(s => s.BirdId == story.BirdId && s.IsHighlighted);
                if (count >= 3) return BadRequest("Maximum of 3 highlights allowed");

                // If pin requested, set highlight order to 1 and bump others
                if (dto.PinToProfile)
                {
                    var existing = await _db.Stories.Where(s => s.BirdId == story.BirdId && s.IsHighlighted).ToListAsync();
                    foreach (var ex in existing)
                    {
                        ex.HighlightOrder = (ex.HighlightOrder ?? 1) + 1;
                    }
                    story.HighlightOrder = 1;
                }
                else
                {
                    // set next order
                    int? maxOrderNullable = await _db.Stories.Where(s => s.BirdId == story.BirdId && s.IsHighlighted && s.HighlightOrder != null).MaxAsync(s => (int?)s.HighlightOrder);
                    var nextOrder = (maxOrderNullable.HasValue) ? maxOrderNullable.Value + 1 : 1;
                    story.HighlightOrder = nextOrder;
                }
            }
            else
            {
                // Clearing highlight, compact orders
                if (story.IsHighlighted && story.HighlightOrder != null)
                {
                    var currentOrder = story.HighlightOrder.Value;
                    var others = await _db.Stories.Where(s => s.BirdId == story.BirdId && s.IsHighlighted && s.HighlightOrder > currentOrder).ToListAsync();
                    foreach (var o in others)
                    {
                        o.HighlightOrder = o.HighlightOrder - 1;
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
