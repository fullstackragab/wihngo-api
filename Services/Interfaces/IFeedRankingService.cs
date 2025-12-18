using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for ranking and personalizing feed content.
    /// </summary>
    public interface IFeedRankingService
    {
        /// <summary>
        /// Get a personalized ranked feed for a user.
        /// </summary>
        Task<PagedResult<RankedStoryDto>> GetRankedFeedAsync(Guid? userId, FeedRequestDto request);

        /// <summary>
        /// Get a specific feed section.
        /// </summary>
        /// <param name="userId">User ID (optional for anonymous)</param>
        /// <param name="sectionType">Section type: from_your_area, in_your_language, discover_worldwide, followed_birds</param>
        /// <param name="limit">Maximum number of stories to return</param>
        Task<FeedSectionDto> GetFeedSectionAsync(Guid? userId, string sectionType, int limit = 10);

        /// <summary>
        /// Get all feed sections for the home screen.
        /// </summary>
        Task<List<FeedSectionDto>> GetAllFeedSectionsAsync(Guid? userId, int storiesPerSection = 5);

        /// <summary>
        /// Get trending stories globally.
        /// </summary>
        Task<List<RankedStoryDto>> GetTrendingStoriesAsync(int limit = 10);
    }
}
