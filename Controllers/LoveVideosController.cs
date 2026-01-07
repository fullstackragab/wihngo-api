using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Love videos express love. Allocation ignores attention.
///
/// Controller for Love Videos (YouTube community submissions).
/// This is SEPARATE from the existing StoriesController (bird-owner content).
///
/// CRITICAL INVARIANTS:
/// - LoveVideos are non-ownable (submitter has no special permissions)
/// - LoveVideos are non-allocating (cannot influence money routing)
/// - LoveVideos have no popularity metrics (no views, likes, comments)
/// - LoveVideos are not linked to birds
/// - All support triggered from LoveVideos goes to the global pool
///
/// Endpoints:
/// - GET /api/love-videos - List approved love videos (public)
/// - GET /api/love-videos/{id} - Get single love video (public)
/// - POST /api/love-videos - Submit new love video (authenticated, creates pending)
/// - POST /api/love-videos/{id}/approve - Approve love video (admin only)
/// - POST /api/love-videos/{id}/reject - Reject love video (admin only)
/// - GET /api/love-videos/pending - List pending love videos (admin only)
/// </summary>
[ApiController]
[Route("api/love-videos")]
public class LoveVideosController : ControllerBase
{
    private readonly ILoveVideoService _loveVideoService;
    private readonly ILogger<LoveVideosController> _logger;

    public LoveVideosController(
        ILoveVideoService loveVideoService,
        ILogger<LoveVideosController> logger)
    {
        _loveVideoService = loveVideoService;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    // ===========================================
    // PUBLIC ENDPOINTS (no auth required)
    // ===========================================

    /// <summary>
    /// List approved love videos (public)
    /// </summary>
    /// <remarks>
    /// Returns approved community love videos with optional filtering by category.
    /// Videos are sorted by most recent by default.
    ///
    /// IMPORTANT: Love videos have NO popularity metrics - no views, likes, or comments.
    /// This is by design to prevent content competition.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoveVideosListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoveVideosListResponse>> ListLoveVideos(
        [FromQuery] string? category = null,
        [FromQuery] string sort = "recent",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!string.IsNullOrEmpty(category) && !LoveVideoCategory.IsValid(category))
            {
                return BadRequest(new
                {
                    error = LoveVideoErrorCodes.InvalidCategory,
                    message = $"Invalid category. Must be one of: {string.Join(", ", LoveVideoCategory.All)}"
                });
            }

            var query = new LoveVideosQueryParams
            {
                Category = category,
                Sort = sort,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 50)
            };

            var result = await _loveVideoService.ListLoveVideosAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing love videos");
            return StatusCode(500, new { error = LoveVideoErrorCodes.InternalError, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get a single approved love video by ID (public)
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoveVideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoveVideoResponse>> GetLoveVideo(Guid id)
    {
        try
        {
            var video = await _loveVideoService.GetLoveVideoAsync(id);
            if (video == null)
            {
                return NotFound(new { error = LoveVideoErrorCodes.LoveVideoNotFound, message = "Love video not found" });
            }
            return Ok(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting love video {Id}", id);
            return StatusCode(500, new { error = LoveVideoErrorCodes.InternalError, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get available love video categories
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public ActionResult<string[]> GetCategories()
    {
        return Ok(LoveVideoCategory.All);
    }

    // ===========================================
    // SUBMISSION ENDPOINTS (authenticated)
    // ===========================================

    /// <summary>
    /// Upload media (image or video) for a love video submission
    /// </summary>
    /// <remarks>
    /// Upload an image or video file directly. Returns a media key and URL to use
    /// when submitting the love video.
    ///
    /// Supported formats:
    /// - Images: JPEG, PNG, GIF, WebP (max 10 MB)
    /// - Videos: MP4, MOV, WebM, AVI (max 50 MB)
    ///
    /// After uploading, use the returned mediaKey in the POST /api/love-videos request.
    /// </remarks>
    [HttpPost("upload-media")]
    [Authorize]
    [RequestSizeLimit(52_428_800)] // 50 MB max
    [ProducesResponseType(typeof(UploadLoveVideoMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadLoveVideoMediaResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadLoveVideoMediaResponse>> UploadMedia(IFormFile file)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new UploadLoveVideoMediaResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.Unauthorized,
                    Message = "Authentication required"
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new UploadLoveVideoMediaResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.NoMediaProvided,
                    Message = "No file provided"
                });
            }

            using var stream = file.OpenReadStream();
            var result = await _loveVideoService.UploadMediaAsync(
                userId.Value,
                stream,
                file.ContentType,
                file.Length);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Love video media uploaded by user {UserId}: {MediaKey}", userId, result.MediaKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading love video media");
            return StatusCode(500, new UploadLoveVideoMediaResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "An error occurred uploading your media"
            });
        }
    }

    /// <summary>
    /// Submit a new love video for moderation
    /// </summary>
    /// <remarks>
    /// Submit a YouTube video link OR uploaded media as a community love video.
    /// The video will be in "pending" status until reviewed by a moderator.
    ///
    /// You must provide either:
    /// - youtubeUrl: A valid YouTube video URL, OR
    /// - mediaKey: A key returned from POST /api/love-videos/upload-media
    ///
    /// For media uploads, title is required. For YouTube, title is auto-fetched.
    ///
    /// IMPORTANT: Submitting a video does NOT grant ownership.
    /// The submitter has no special permissions after submission.
    ///
    /// Categories:
    /// - love_companionship: Videos showing love and companionship with birds
    /// - respect_for_birds: Videos showing respect for birds and their dignity
    /// - everyday_care: Videos showing everyday bird care practices
    ///
    /// Content that will be REJECTED:
    /// - Content asking for donations
    /// - Content using "my bird" / ownership framing
    /// - Content with urgency manipulation ("save this bird now")
    /// - Content directing funds to a specific bird
    /// - Promotional or influencer-driven content
    /// </remarks>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SubmitLoveVideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SubmitLoveVideoResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitLoveVideoResponse>> SubmitLoveVideo([FromBody] SubmitLoveVideoRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new SubmitLoveVideoResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.Unauthorized,
                    Message = "Authentication required"
                });
            }

            var result = await _loveVideoService.SubmitLoveVideoAsync(userId.Value, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Love video submitted: {Id} by user {UserId}", result.Id, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting love video");
            return StatusCode(500, new SubmitLoveVideoResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "An error occurred submitting your video"
            });
        }
    }

    // ===========================================
    // MODERATION ENDPOINTS (admin only)
    // ===========================================

    /// <summary>
    /// Get love videos pending moderation (admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(List<LoveVideoModerationItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LoveVideoModerationItem>>> GetPendingLoveVideos()
    {
        try
        {
            var videos = await _loveVideoService.GetPendingLoveVideosAsync();
            return Ok(videos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending love videos");
            return StatusCode(500, new { error = LoveVideoErrorCodes.InternalError, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get a single love video for moderation (any status, admin only)
    /// </summary>
    [HttpGet("moderation/{id}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(LoveVideoModerationItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoveVideoModerationItem>> GetLoveVideoForModeration(Guid id)
    {
        try
        {
            var video = await _loveVideoService.GetLoveVideoForModerationAsync(id);
            if (video == null)
            {
                return NotFound(new { error = LoveVideoErrorCodes.LoveVideoNotFound, message = "Love video not found" });
            }
            return Ok(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting love video for moderation {Id}", id);
            return StatusCode(500, new { error = LoveVideoErrorCodes.InternalError, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Approve a pending love video (admin only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(LoveVideoModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoveVideoModerationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoveVideoModerationResponse>> ApproveLoveVideo(Guid id, [FromBody] ApproveLoveVideoRequest? request = null)
    {
        try
        {
            var moderatorId = GetUserId();
            if (moderatorId == null)
            {
                return Unauthorized(new LoveVideoModerationResponse
                {
                    Success = false,
                    LoveVideoId = id,
                    ErrorCode = LoveVideoErrorCodes.Unauthorized
                });
            }

            var result = await _loveVideoService.ApproveLoveVideoAsync(id, moderatorId.Value, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving love video {Id}", id);
            return StatusCode(500, new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = id,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Reject a pending love video (admin only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(LoveVideoModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoveVideoModerationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoveVideoModerationResponse>> RejectLoveVideo(Guid id, [FromBody] RejectLoveVideoRequest request)
    {
        try
        {
            var moderatorId = GetUserId();
            if (moderatorId == null)
            {
                return Unauthorized(new LoveVideoModerationResponse
                {
                    Success = false,
                    LoveVideoId = id,
                    ErrorCode = LoveVideoErrorCodes.Unauthorized
                });
            }

            var result = await _loveVideoService.RejectLoveVideoAsync(id, moderatorId.Value, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting love video {Id}", id);
            return StatusCode(500, new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = id,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Hide an approved love video (admin only)
    /// </summary>
    [HttpPost("{id}/hide")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(LoveVideoModerationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoveVideoModerationResponse>> HideLoveVideo(Guid id, [FromBody] RejectLoveVideoRequest request)
    {
        try
        {
            var moderatorId = GetUserId();
            if (moderatorId == null)
            {
                return Unauthorized(new LoveVideoModerationResponse
                {
                    Success = false,
                    LoveVideoId = id,
                    ErrorCode = LoveVideoErrorCodes.Unauthorized
                });
            }

            var result = await _loveVideoService.HideLoveVideoAsync(id, moderatorId.Value, request.Reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding love video {Id}", id);
            return StatusCode(500, new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = id,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }
}
