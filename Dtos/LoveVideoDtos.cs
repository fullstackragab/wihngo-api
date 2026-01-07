using System.ComponentModel.DataAnnotations;
using Wihngo.Models.Entities;

namespace Wihngo.Dtos;

/// <summary>
/// Love videos express love. Allocation ignores attention.
///
/// DTOs for the Love Videos feature (YouTube community submissions).
/// These DTOs enforce the invariant that love videos have no popularity metrics,
/// no bird associations, and no ownership concepts.
///
/// NOTE: This is SEPARATE from the existing "stories" feature (bird-owner content).
/// </summary>

// ===========================================
// PUBLIC RESPONSE DTOs
// ===========================================

/// <summary>
/// Public love video response (for approved videos only)
/// </summary>
public class LoveVideoResponse
{
    public Guid Id { get; set; }

    /// <summary>
    /// YouTube video URL for embedding (null if using direct media)
    /// </summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Direct media URL (null if using YouTube)
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Media type: image, video (null if using YouTube)
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Short neutral description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Status: pending, approved, rejected
    /// </summary>
    public string Status { get; set; } = LoveVideoStatus.Approved;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the video was approved (null if not yet approved)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// YouTube thumbnail URL (derived from video ID)
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    // ===========================================
    // EXPLICIT EXCLUSIONS (these fields must NEVER be added)
    // ===========================================
    // - viewCount, likeCount, shareCount, commentCount
    // - birdId, birdName (no bird association)
    // - ownerUserId, ownerName (no ownership)
    // - ranking, score, trending
    // - donationTotal, supportTotal
}

/// <summary>
/// Paged list of love videos
/// </summary>
public class LoveVideosListResponse
{
    public List<LoveVideoResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ===========================================
// SUBMISSION DTOs
// ===========================================

/// <summary>
/// Request to submit a new love video (YouTube video OR direct media upload)
/// Must provide at least one of: YoutubeUrl, MediaKey, or Description.
/// </summary>
public class SubmitLoveVideoRequest
{
    /// <summary>
    /// YouTube video URL (optional)
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// S3 key from media upload (optional)
    /// </summary>
    [MaxLength(500)]
    public string? MediaKey { get; set; }

    /// <summary>
    /// Optional short description (max 500 chars)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    // ===========================================
    // EXPLICIT EXCLUSIONS (these fields must NEVER be added)
    // ===========================================
    // - birdId (stories cannot be linked to birds)
    // - ownershipClaim (stories are not owned)
}

/// <summary>
/// Response after submitting a love video
/// </summary>
public class SubmitLoveVideoResponse
{
    public bool Success { get; set; }

    /// <summary>
    /// The created love video ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Status will always be "pending" for new submissions
    /// </summary>
    public string Status { get; set; } = LoveVideoStatus.Pending;

    /// <summary>
    /// User-friendly message
    /// </summary>
    public string? Message { get; set; }

    public string? ErrorCode { get; set; }
}

// ===========================================
// MODERATION DTOs (Admin only)
// ===========================================

/// <summary>
/// Love video for moderation queue (includes pending status and submitter info)
/// </summary>
public class LoveVideoModerationItem
{
    public Guid Id { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? YoutubeVideoId { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Submitter user ID (for audit, NOT ownership)
    /// </summary>
    public Guid SubmittedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? RejectionReason { get; set; }

    // ===========================================
    // AI MODERATION FIELDS
    // ===========================================

    /// <summary>
    /// AI decision: auto_approve, needs_human_review, reject
    /// </summary>
    public string? AiDecision { get; set; }

    /// <summary>
    /// AI confidence score (0.0 to 1.0)
    /// </summary>
    public double? AiConfidence { get; set; }

    /// <summary>
    /// AI-generated flags (e.g., safe, spam, off_topic)
    /// </summary>
    public List<string>? AiFlags { get; set; }

    /// <summary>
    /// AI-generated reasons for the decision
    /// </summary>
    public List<string>? AiReasons { get; set; }
}

/// <summary>
/// Request to approve a love video
/// </summary>
public class ApproveLoveVideoRequest
{
    /// <summary>
    /// Optional: Override the description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Request to reject a love video
/// </summary>
public class RejectLoveVideoRequest
{
    /// <summary>
    /// Reason for rejection (required)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response for love video moderation actions
/// </summary>
public class LoveVideoModerationResponse
{
    public bool Success { get; set; }
    public Guid LoveVideoId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

// ===========================================
// QUERY DTOs
// ===========================================

/// <summary>
/// Query parameters for listing love videos
/// </summary>
public class LoveVideosQueryParams
{
    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Sort by: recent (default), curated
    /// </summary>
    public string Sort { get; set; } = "recent";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ===========================================
// MEDIA UPLOAD DTOs
// ===========================================

/// <summary>
/// Response after uploading media for a love video
/// </summary>
public class UploadLoveVideoMediaResponse
{
    public bool Success { get; set; }

    /// <summary>
    /// S3 key to use in SubmitLoveVideoRequest.MediaKey
    /// </summary>
    public string? MediaKey { get; set; }

    /// <summary>
    /// Public URL of the uploaded media
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Media type: image, video
    /// </summary>
    public string? MediaType { get; set; }

    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

// ===========================================
// ERROR CODES
// ===========================================

public static class LoveVideoErrorCodes
{
    public const string InvalidYoutubeUrl = "INVALID_YOUTUBE_URL";
    public const string InvalidCategory = "INVALID_CATEGORY";
    public const string LoveVideoNotFound = "LOVE_VIDEO_NOT_FOUND";
    public const string AlreadyApproved = "ALREADY_APPROVED";
    public const string AlreadyRejected = "ALREADY_REJECTED";
    public const string NotPending = "NOT_PENDING";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string ContentViolation = "CONTENT_VIOLATION";
    public const string DuplicateUrl = "DUPLICATE_URL";
    public const string InternalError = "INTERNAL_ERROR";

    // Media upload errors
    public const string InvalidMediaType = "INVALID_MEDIA_TYPE";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string NoMediaProvided = "NO_MEDIA_PROVIDED";
    public const string MustProvideYoutubeOrMedia = "MUST_PROVIDE_YOUTUBE_OR_MEDIA";
    public const string CannotProvideBoth = "CANNOT_PROVIDE_BOTH_YOUTUBE_AND_MEDIA";
    public const string TitleRequiredForMedia = "TITLE_REQUIRED_FOR_MEDIA";
    public const string InvalidMediaKey = "INVALID_MEDIA_KEY";
    public const string MediaNotFound = "MEDIA_NOT_FOUND";

    // Content moderation rejection reasons
    public const string AsksForDonations = "ASKS_FOR_DONATIONS";
    public const string OwnershipFraming = "OWNERSHIP_FRAMING";
    public const string UrgencyManipulation = "URGENCY_MANIPULATION";
    public const string BirdSpecificFundraising = "BIRD_SPECIFIC_FUNDRAISING";
    public const string PromotionalContent = "PROMOTIONAL_CONTENT";
}
