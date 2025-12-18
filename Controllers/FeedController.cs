using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers
{
    /// <summary>
    /// Controller for personalized feed endpoints.
    /// Provides ranked, sectioned content based on user preferences.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FeedController : ControllerBase
    {
        private readonly IFeedRankingService _feedService;
        private readonly ILogger<FeedController> _logger;

        public FeedController(
            IFeedRankingService feedService,
            ILogger<FeedController> logger)
        {
            _feedService = feedService;
            _logger = logger;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        /// <summary>
        /// Get personalized ranked feed.
        /// GET /api/feed
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<RankedStoryDto>>> GetFeed(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? language = null,
            [FromQuery] string? country = null)
        {
            var userId = GetUserIdClaim();

            var request = new FeedRequestDto
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50),
                Language = language,
                Country = country
            };

            var result = await _feedService.GetRankedFeedAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// Get all feed sections for the home screen.
        /// GET /api/feed/sections
        /// </summary>
        [HttpGet("sections")]
        public async Task<ActionResult<List<FeedSectionDto>>> GetAllSections([FromQuery] int storiesPerSection = 5)
        {
            var userId = GetUserIdClaim();
            var limit = Math.Max(1, Math.Min(storiesPerSection, 20));

            var sections = await _feedService.GetAllFeedSectionsAsync(userId, limit);
            return Ok(sections);
        }

        /// <summary>
        /// Get a specific feed section.
        /// GET /api/feed/section/{sectionType}
        /// </summary>
        /// <param name="sectionType">Section type: from_your_area, in_your_language, discover_worldwide, followed_birds</param>
        /// <param name="limit">Maximum number of stories to return</param>
        [HttpGet("section/{sectionType}")]
        public async Task<ActionResult<FeedSectionDto>> GetSection(string sectionType, [FromQuery] int limit = 10)
        {
            var userId = GetUserIdClaim();

            try
            {
                var section = await _feedService.GetFeedSectionAsync(userId, sectionType, Math.Min(limit, 50));
                return Ok(section);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get trending stories globally.
        /// GET /api/feed/trending
        /// </summary>
        [HttpGet("trending")]
        public async Task<ActionResult<List<RankedStoryDto>>> GetTrending([FromQuery] int limit = 10)
        {
            var result = await _feedService.GetTrendingStoriesAsync(Math.Min(limit, 50));
            return Ok(result);
        }
    }
}
