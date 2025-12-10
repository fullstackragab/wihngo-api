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

        public StoriesController(AppDbContext db, IMapper mapper, INotificationService notificationService)
        {
            _db = db;
            _mapper = mapper;
            _notificationService = notificationService;
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

            var dtoItems = _mapper.Map<IEnumerable<StorySummaryDto>>(items);

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
            return Ok(_mapper.Map<StoryReadDto>(story));
        }

        [HttpPost]
        public async Task<ActionResult<StoryReadDto>> Post([FromBody] Story story)
        {
            story.StoryId = Guid.NewGuid();
            story.CreatedAt = DateTime.UtcNow;
            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            var created = await _db.Stories.Include(s => s.Bird).Include(s => s.Author).FirstOrDefaultAsync(s => s.StoryId == story.StoryId);

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

            return CreatedAtAction(nameof(Get), new { id = story.StoryId }, _mapper.Map<StoryReadDto>(created));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] Story updated)
        {
            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            story.Content = updated.Content;
            story.ImageUrl = updated.ImageUrl;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
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
