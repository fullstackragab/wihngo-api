using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for managing Kind Words - a constrained, care-focused comment system.
    /// Kind words are supportive messages left on bird stories by supporters or followers.
    /// </summary>
    public interface IKindWordsService
    {
        /// <summary>
        /// Get kind words section for a bird, including eligibility to post.
        /// </summary>
        /// <param name="birdId">The bird ID</param>
        /// <param name="currentUserId">The current user (null if anonymous)</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        Task<KindWordsSectionDto> GetKindWordsSectionAsync(Guid birdId, Guid? currentUserId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Post a kind word on a bird.
        /// Validates: user eligibility, text constraints, rate limits.
        /// </summary>
        /// <param name="birdId">The bird ID</param>
        /// <param name="authorUserId">The author's user ID</param>
        /// <param name="dto">The kind word content</param>
        Task<KindWordResultDto> PostKindWordAsync(Guid birdId, Guid authorUserId, KindWordCreateDto dto);

        /// <summary>
        /// Delete a kind word (bird owner or admin only).
        /// Silent soft delete - no notification to author.
        /// </summary>
        /// <param name="kindWordId">The kind word ID</param>
        /// <param name="requestingUserId">The user requesting deletion</param>
        Task<KindWordResultDto> DeleteKindWordAsync(Guid kindWordId, Guid requestingUserId);

        /// <summary>
        /// Toggle kind words on/off for a bird (owner only).
        /// </summary>
        /// <param name="birdId">The bird ID</param>
        /// <param name="ownerUserId">The bird owner's user ID</param>
        /// <param name="enabled">Whether to enable kind words</param>
        Task<KindWordResultDto> SetKindWordsEnabledAsync(Guid birdId, Guid ownerUserId, bool enabled);

        /// <summary>
        /// Block a user from posting kind words on a specific bird.
        /// Silent block - no notification to blocked user.
        /// </summary>
        /// <param name="birdId">The bird ID</param>
        /// <param name="ownerUserId">The bird owner's user ID</param>
        /// <param name="userToBlockId">The user to block</param>
        Task<KindWordResultDto> BlockUserAsync(Guid birdId, Guid ownerUserId, Guid userToBlockId);

        /// <summary>
        /// Unblock a user from posting kind words on a specific bird.
        /// </summary>
        /// <param name="birdId">The bird ID</param>
        /// <param name="ownerUserId">The bird owner's user ID</param>
        /// <param name="userToUnblockId">The user to unblock</param>
        Task<KindWordResultDto> UnblockUserAsync(Guid birdId, Guid ownerUserId, Guid userToUnblockId);

        /// <summary>
        /// Check if user can post kind words on a bird.
        /// Must have supported OR follow the bird, and not be blocked.
        /// </summary>
        Task<(bool CanPost, string? Reason)> CanUserPostAsync(Guid birdId, Guid userId);
    }
}
