using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Love videos express love. Allocation ignores attention.
///
/// A LoveVideo is a YouTube video submission that expresses love, respect, and companionship with birds.
/// This is SEPARATE from the existing "stories" feature (which are bird-owner created content).
///
/// CRITICAL INVARIANTS (must never be violated):
/// - LoveVideos are NON-OWNABLE entities (no ownership concept)
/// - LoveVideos are NON-ALLOCATING (cannot influence money routing)
/// - LoveVideos have NO popularity metrics (no views, likes, comments, shares)
/// - LoveVideos are NOT linked to birds (no birdId)
/// - All support from LoveVideos goes to the global pool, minimum-first
///
/// What LoveVideos ARE:
/// - Educational/emotional content about bird care and companionship
/// - Community submissions that require moderation
/// - A way to express love without creating competition
/// </summary>
[Table("love_videos")]
public class LoveVideo
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// YouTube video URL (optional - either YouTube URL OR direct media upload)
    /// </summary>
    [Column("youtube_url")]
    [MaxLength(500)]
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Extracted YouTube video ID for embedding
    /// </summary>
    [Column("youtube_video_id")]
    [MaxLength(20)]
    public string? YoutubeVideoId { get; set; }

    /// <summary>
    /// Short neutral description (user-provided, moderated)
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category: love_companionship, respect_for_birds, everyday_care
    /// </summary>
    [Column("category")]
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = LoveVideoCategory.LoveCompanionship;

    /// <summary>
    /// Moderation status: pending, approved, rejected
    /// </summary>
    [Column("status")]
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = LoveVideoStatus.Pending;

    /// <summary>
    /// User who submitted the story (for audit/moderation only, NOT ownership)
    /// The submitter has NO special permissions after submission.
    /// </summary>
    [Column("submitted_by_user_id")]
    public Guid SubmittedByUserId { get; set; }

    /// <summary>
    /// Reason for rejection (if status = rejected)
    /// </summary>
    [Column("rejection_reason")]
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Moderator who approved/rejected the story
    /// </summary>
    [Column("moderated_by_user_id")]
    public Guid? ModeratedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ===========================================
    // MEDIA SUPPORT (direct uploads to public S3)
    // ===========================================

    /// <summary>
    /// S3 key for uploaded media (image or video)
    /// This is an ALTERNATIVE to YoutubeUrl - a love video has either YouTube OR direct media
    /// </summary>
    [Column("media_key")]
    [MaxLength(500)]
    public string? MediaKey { get; set; }

    /// <summary>
    /// Public URL for the uploaded media (derived from MediaKey)
    /// </summary>
    [Column("media_url")]
    [MaxLength(500)]
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Type of uploaded media: image, video
    /// </summary>
    [Column("media_type")]
    [MaxLength(20)]
    public string? MediaType { get; set; }

    // ===========================================
    // EXPLICIT EXCLUSIONS (these fields must NEVER exist)
    // ===========================================
    // - birdId (stories cannot be linked to birds)
    // - ownerUserId (stories are not owned)
    // - viewCount, likeCount, shareCount, commentCount (no popularity metrics)
    // - donationTotal, supportTotal (stories cannot receive/track money)
    // - ranking, score, trending (no competition)

    /// <summary>
    /// Extract YouTube video ID from various URL formats
    /// </summary>
    public static string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Handle various YouTube URL formats:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        // - https://www.youtube.com/v/VIDEO_ID

        try
        {
            var uri = new Uri(url);

            // youtu.be format
            if (uri.Host.Contains("youtu.be"))
            {
                return uri.AbsolutePath.TrimStart('/').Split('?')[0];
            }

            // youtube.com formats
            if (uri.Host.Contains("youtube.com"))
            {
                // /watch?v=VIDEO_ID
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var videoId = query["v"];
                if (!string.IsNullOrEmpty(videoId))
                    return videoId;

                // /embed/VIDEO_ID or /v/VIDEO_ID
                var segments = uri.AbsolutePath.Split('/');
                if (segments.Length >= 2)
                {
                    var lastSegment = segments[^1];
                    if (!string.IsNullOrEmpty(lastSegment) && lastSegment.Length == 11)
                        return lastSegment;
                }
            }
        }
        catch
        {
            // Invalid URL format
        }

        return null;
    }
}

/// <summary>
/// Love video category constants
/// </summary>
public static class LoveVideoCategory
{
    /// <summary>
    /// Videos showing love and companionship with birds
    /// </summary>
    public const string LoveCompanionship = "love_companionship";

    /// <summary>
    /// Videos showing respect for birds and their dignity
    /// </summary>
    public const string RespectForBirds = "respect_for_birds";

    /// <summary>
    /// Videos showing everyday bird care practices
    /// </summary>
    public const string EverydayCare = "everyday_care";

    public static bool IsValid(string category)
    {
        return category == LoveCompanionship ||
               category == RespectForBirds ||
               category == EverydayCare;
    }

    public static readonly string[] All = { LoveCompanionship, RespectForBirds, EverydayCare };
}

/// <summary>
/// Love video moderation status constants
/// </summary>
public static class LoveVideoStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static bool IsValid(string status)
    {
        return status == Pending || status == Approved || status == Rejected;
    }
}

/// <summary>
/// Love video media type constants (for direct uploads)
/// </summary>
public static class LoveVideoMediaType
{
    public const string Image = "image";
    public const string Video = "video";

    public static bool IsValid(string? mediaType)
    {
        return mediaType == Image || mediaType == Video;
    }

    public static string? FromContentType(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Image;
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return Video;
        return null;
    }
}
