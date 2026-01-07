using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Love videos express love. Allocation ignores attention.
///
/// Service for managing Love Videos (YouTube community submissions).
/// This is SEPARATE from the existing "stories" feature (bird-owner content).
///
/// CRITICAL INVARIANTS:
/// - LoveVideos are non-ownable (submitter has no special permissions)
/// - LoveVideos are non-allocating (cannot influence money routing)
/// - LoveVideos have no popularity metrics
/// - LoveVideos are not linked to birds
/// - All support from LoveVideos goes to the global pool
/// </summary>
public interface ILoveVideoService
{
    // ===========================================
    // PUBLIC READ OPERATIONS
    // ===========================================

    /// <summary>
    /// Get a single approved love video by ID (public)
    /// </summary>
    Task<LoveVideoResponse?> GetLoveVideoAsync(Guid loveVideoId);

    /// <summary>
    /// List approved love videos with optional filtering and pagination (public)
    /// </summary>
    Task<LoveVideosListResponse> ListLoveVideosAsync(LoveVideosQueryParams query);

    // ===========================================
    // SUBMISSION OPERATIONS (authenticated users)
    // ===========================================

    /// <summary>
    /// Upload media (image/video) for a love video submission.
    /// Returns the S3 key and URL to use in the subsequent submit request.
    /// </summary>
    /// <param name="userId">The uploading user</param>
    /// <param name="fileStream">The file content stream</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="fileSize">Size of the file in bytes</param>
    /// <returns>Upload result with S3 key and URL</returns>
    Task<UploadLoveVideoMediaResponse> UploadMediaAsync(Guid userId, Stream fileStream, string contentType, long fileSize);

    /// <summary>
    /// Submit a new love video for moderation.
    /// Status will be 'pending' until approved by a moderator.
    /// </summary>
    /// <param name="userId">The submitting user (for audit only, NOT ownership)</param>
    /// <param name="request">Submission details</param>
    Task<SubmitLoveVideoResponse> SubmitLoveVideoAsync(Guid userId, SubmitLoveVideoRequest request);

    /// <summary>
    /// Check if a YouTube URL has already been submitted
    /// </summary>
    Task<bool> IsUrlAlreadySubmittedAsync(string youtubeUrl);

    /// <summary>
    /// Check if a media key (S3 path) is valid for love video uploads
    /// </summary>
    bool IsValidMediaKey(string mediaKey);

    /// <summary>
    /// Generate the public URL for a love video media key
    /// </summary>
    string GetMediaUrl(string mediaKey);

    // ===========================================
    // MODERATION OPERATIONS (admin only)
    // ===========================================

    /// <summary>
    /// Get love videos pending moderation
    /// </summary>
    Task<List<LoveVideoModerationItem>> GetPendingLoveVideosAsync();

    /// <summary>
    /// Get a single love video for moderation (any status)
    /// </summary>
    Task<LoveVideoModerationItem?> GetLoveVideoForModerationAsync(Guid loveVideoId);

    /// <summary>
    /// Approve a pending love video
    /// </summary>
    Task<LoveVideoModerationResponse> ApproveLoveVideoAsync(Guid loveVideoId, Guid moderatorUserId, ApproveLoveVideoRequest? request = null);

    /// <summary>
    /// Reject a pending love video
    /// </summary>
    Task<LoveVideoModerationResponse> RejectLoveVideoAsync(Guid loveVideoId, Guid moderatorUserId, RejectLoveVideoRequest request);

    /// <summary>
    /// Hide an approved love video (soft delete)
    /// </summary>
    Task<LoveVideoModerationResponse> HideLoveVideoAsync(Guid loveVideoId, Guid moderatorUserId, string reason);

    // ===========================================
    // VALIDATION HELPERS
    // ===========================================

    /// <summary>
    /// Validate that a URL is a valid YouTube video URL
    /// </summary>
    bool IsValidYoutubeUrl(string url);

    /// <summary>
    /// Extract YouTube video ID from URL
    /// </summary>
    string? ExtractYoutubeVideoId(string url);

    /// <summary>
    /// Fetch video metadata from YouTube (title, thumbnail)
    /// </summary>
    Task<YouTubeVideoMetadata?> FetchYoutubeMetadataAsync(string videoId);
}

/// <summary>
/// YouTube video metadata fetched from API
/// </summary>
public class YouTubeVideoMetadata
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ChannelName { get; set; }
    public DateTime? PublishedAt { get; set; }
}
