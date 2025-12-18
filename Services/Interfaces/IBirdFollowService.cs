namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for managing bird follows for personalized feed content.
    /// Different from "Love" - following means wanting to see stories about this bird in the feed.
    /// </summary>
    public interface IBirdFollowService
    {
        /// <summary>
        /// Follow a bird to see its stories in the feed.
        /// </summary>
        Task FollowBirdAsync(Guid userId, Guid birdId);

        /// <summary>
        /// Unfollow a bird to stop seeing its stories prominently.
        /// </summary>
        Task UnfollowBirdAsync(Guid userId, Guid birdId);

        /// <summary>
        /// Check if a user is following a specific bird.
        /// </summary>
        Task<bool> IsFollowingAsync(Guid userId, Guid birdId);

        /// <summary>
        /// Get all bird IDs that a user is following.
        /// </summary>
        Task<List<Guid>> GetFollowedBirdIdsAsync(Guid userId);

        /// <summary>
        /// Get the count of followers for a bird.
        /// </summary>
        Task<int> GetFollowerCountAsync(Guid birdId);
    }
}
