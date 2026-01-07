using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Love videos express love. Allocation ignores attention.
///
/// Service for managing Love Videos (YouTube community submissions OR direct media uploads).
/// This is SEPARATE from the existing "stories" feature (bird-owner content).
///
/// CRITICAL INVARIANTS:
/// - LoveVideos are non-ownable (submitter has no special permissions)
/// - LoveVideos are non-allocating (cannot influence money routing)
/// - LoveVideos have no popularity metrics
/// - LoveVideos are not linked to birds
/// - All support from LoveVideos goes to the global pool
/// </summary>
public class LoveVideoService : ILoveVideoService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IS3Service _s3Service;
    private readonly IAiModerationService _aiModerationService;
    private readonly AwsConfiguration _awsConfig;
    private readonly AwsPublicBucketConfiguration _publicBucketConfig;
    private readonly ILogger<LoveVideoService> _logger;

    // Max file sizes
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const long MaxVideoSizeBytes = 50 * 1024 * 1024; // 50 MB

    // Allowed content types
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
    };

    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime", "video/webm", "video/x-msvideo"
    };

    private static readonly Regex YoutubeUrlRegex = new(
        @"^(https?:\/\/)?(www\.)?(youtube\.com\/(watch\?v=|embed\/|v\/)|youtu\.be\/)[\w-]{11}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public LoveVideoService(
        IDbConnectionFactory dbFactory,
        IS3Service s3Service,
        IAiModerationService aiModerationService,
        IOptions<AwsConfiguration> awsConfig,
        IOptions<AwsPublicBucketConfiguration> publicBucketConfig,
        ILogger<LoveVideoService> logger)
    {
        _dbFactory = dbFactory;
        _s3Service = s3Service;
        _aiModerationService = aiModerationService;
        _awsConfig = awsConfig.Value;
        _publicBucketConfig = publicBucketConfig.Value;
        _logger = logger;
    }

    public async Task<LoveVideoResponse?> GetLoveVideoAsync(Guid loveVideoId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var video = await conn.QueryFirstOrDefaultAsync<LoveVideo>(
            @"SELECT * FROM love_videos
              WHERE id = @Id AND status = @ApprovedStatus",
            new { Id = loveVideoId, ApprovedStatus = LoveVideoStatus.Approved });

        return video == null ? null : MapToResponse(video);
    }

    public async Task<LoveVideosListResponse> ListLoveVideosAsync(LoveVideosQueryParams query)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var offset = (query.Page - 1) * query.PageSize;
        var whereClauses = new List<string> { "status = @ApprovedStatus" };
        var parameters = new DynamicParameters();
        parameters.Add("ApprovedStatus", LoveVideoStatus.Approved);

        if (!string.IsNullOrEmpty(query.Category) && LoveVideoCategory.IsValid(query.Category))
        {
            whereClauses.Add("category = @Category");
            parameters.Add("Category", query.Category);
        }

        var whereClause = string.Join(" AND ", whereClauses);
        var orderBy = query.Sort?.ToLower() switch
        {
            "curated" => "approved_at DESC, created_at DESC",
            _ => "created_at DESC"
        };

        var countSql = $"SELECT COUNT(*) FROM love_videos WHERE {whereClause}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", offset);

        var sql = $@"SELECT * FROM love_videos WHERE {whereClause} ORDER BY {orderBy} LIMIT @Limit OFFSET @Offset";
        var videos = await conn.QueryAsync<LoveVideo>(sql, parameters);

        return new LoveVideosListResponse
        {
            Items = videos.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UploadLoveVideoMediaResponse> UploadMediaAsync(Guid userId, Stream fileStream, string contentType, long fileSize)
    {
        // Validate content type
        var isImage = AllowedImageTypes.Contains(contentType);
        var isVideo = AllowedVideoTypes.Contains(contentType);

        if (!isImage && !isVideo)
        {
            return new UploadLoveVideoMediaResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.InvalidMediaType,
                Message = $"Invalid media type: {contentType}. Allowed: images (jpeg, png, gif, webp) and videos (mp4, quicktime, webm)"
            };
        }

        // Validate file size
        var maxSize = isImage ? MaxImageSizeBytes : MaxVideoSizeBytes;
        if (fileSize > maxSize)
        {
            var maxSizeMb = maxSize / (1024 * 1024);
            return new UploadLoveVideoMediaResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.FileTooLarge,
                Message = $"File too large. Maximum size for {(isImage ? "images" : "videos")} is {maxSizeMb} MB"
            };
        }

        try
        {
            // Determine media type and S3 media type
            var mediaType = isImage ? LoveVideoMediaType.Image : LoveVideoMediaType.Video;
            var s3MediaType = isImage ? "love-video-image" : "love-video-video";

            // Get file extension from content type
            var extension = GetExtensionFromContentType(contentType);

            // Generate upload URL and key using S3 service
            var (_, s3Key) = await _s3Service.GenerateUploadUrlAsync(userId, s3MediaType, extension);

            // Upload the file directly
            await _s3Service.UploadFileAsync(s3Key, fileStream, contentType);

            // Generate public URL
            var mediaUrl = GetMediaUrl(s3Key);

            _logger.LogInformation(
                "Love video media uploaded: {S3Key} by user {UserId}, type: {MediaType}, size: {Size} bytes",
                s3Key, userId, mediaType, fileSize);

            return new UploadLoveVideoMediaResponse
            {
                Success = true,
                MediaKey = s3Key,
                Url = mediaUrl,
                MediaType = mediaType,
                Message = "Media uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading love video media for user {UserId}", userId);
            return new UploadLoveVideoMediaResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.InternalError,
                Message = "Failed to upload media"
            };
        }
    }

    public async Task<SubmitLoveVideoResponse> SubmitLoveVideoAsync(Guid userId, SubmitLoveVideoRequest request)
    {
        var hasYoutube = !string.IsNullOrWhiteSpace(request.YoutubeUrl);
        var hasMedia = !string.IsNullOrWhiteSpace(request.MediaKey);
        var hasDescription = !string.IsNullOrWhiteSpace(request.Description);

        // Validate: must provide at least one of YouTube URL, media key, or description
        if (!hasYoutube && !hasMedia && !hasDescription)
        {
            return new SubmitLoveVideoResponse
            {
                Success = false,
                ErrorCode = LoveVideoErrorCodes.MustProvideYoutubeOrMedia,
                Message = "Please provide a YouTube URL, upload media, or add a description"
            };
        }

        string? youtubeUrl = null;
        string? youtubeVideoId = null;
        string? mediaKey = null;
        string? mediaUrl = null;
        string? mediaType = null;

        if (hasYoutube)
        {
            // YouTube URL submission
            if (!IsValidYoutubeUrl(request.YoutubeUrl))
            {
                return new SubmitLoveVideoResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.InvalidYoutubeUrl,
                    Message = "Please provide a valid YouTube video URL"
                };
            }

            if (await IsUrlAlreadySubmittedAsync(request.YoutubeUrl!))
            {
                return new SubmitLoveVideoResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.DuplicateUrl,
                    Message = "This video has already been submitted"
                };
            }

            youtubeUrl = NormalizeYoutubeUrl(request.YoutubeUrl!);
            youtubeVideoId = ExtractYoutubeVideoId(request.YoutubeUrl!);
        }

        if (hasMedia)
        {
            // Direct media upload submission
            if (!IsValidMediaKey(request.MediaKey!))
            {
                return new SubmitLoveVideoResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.InvalidMediaKey,
                    Message = "Invalid media key"
                };
            }

            // Verify media exists
            if (!await _s3Service.FileExistsAsync(request.MediaKey!))
            {
                return new SubmitLoveVideoResponse
                {
                    Success = false,
                    ErrorCode = LoveVideoErrorCodes.MediaNotFound,
                    Message = "Uploaded media not found. Please upload the media first."
                };
            }

            mediaKey = request.MediaKey;
            mediaUrl = GetMediaUrl(mediaKey);
            mediaType = GetMediaTypeFromKey(mediaKey);
        }

        // Run AI moderation
        var aiResult = await _aiModerationService.ModerateContentAsync(new AiModerationRequest
        {
            StoryId = Guid.NewGuid(), // Will be replaced with actual ID
            UserId = userId,
            UserTrustLevel = UserTrustLevel.New, // TODO: Get from user profile
            Text = request.Description,
            HasImages = hasMedia && mediaType == LoveVideoMediaType.Image,
            HasVideo = hasMedia && mediaType == LoveVideoMediaType.Video,
            HasYoutubeUrl = hasYoutube,
            Language = "auto"
        });

        // Determine status based on AI decision
        var status = aiResult.Decision switch
        {
            AiModerationDecision.AutoApprove => LoveVideoStatus.Approved,
            AiModerationDecision.Reject => LoveVideoStatus.Rejected,
            _ => LoveVideoStatus.Pending // needs_human_review or unknown
        };

        var now = DateTime.UtcNow;

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var loveVideo = new LoveVideo
        {
            Id = Guid.NewGuid(),
            YoutubeUrl = youtubeUrl,
            YoutubeVideoId = youtubeVideoId,
            MediaKey = mediaKey,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            Description = request.Description?.Trim(),
            Category = LoveVideoCategory.LoveCompanionship, // Default category
            Status = status,
            SubmittedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            // AI moderation fields
            AiDecision = aiResult.Decision,
            AiConfidence = aiResult.Confidence,
            AiFlags = aiResult.Flags.Count > 0 ? JsonSerializer.Serialize(aiResult.Flags) : null,
            AiReasons = aiResult.Reasons.Count > 0 ? JsonSerializer.Serialize(aiResult.Reasons) : null,
            AiModeratedAt = now,
            // Set approval time if auto-approved
            ApprovedAt = status == LoveVideoStatus.Approved ? now : null,
            RejectionReason = status == LoveVideoStatus.Rejected
                ? string.Join("; ", aiResult.Reasons)
                : null
        };

        await conn.ExecuteAsync(
            @"INSERT INTO love_videos
              (id, youtube_url, youtube_video_id, media_key, media_url, media_type, description, category, status,
               submitted_by_user_id, created_at, updated_at, approved_at, rejection_reason,
               ai_decision, ai_confidence, ai_flags, ai_reasons, ai_moderated_at)
              VALUES
              (@Id, @YoutubeUrl, @YoutubeVideoId, @MediaKey, @MediaUrl, @MediaType, @Description, @Category, @Status,
               @SubmittedByUserId, @CreatedAt, @UpdatedAt, @ApprovedAt, @RejectionReason,
               @AiDecision, @AiConfidence, @AiFlags, @AiReasons, @AiModeratedAt)",
            loveVideo);

        var source = hasYoutube ? $"YouTube: {youtubeUrl}" : hasMedia ? $"Media: {mediaKey}" : "Description only";
        _logger.LogInformation(
            "Love video submitted: {Id} by user {UserId}, {Source}, Status: {Status}, AI Decision: {AiDecision} ({AiConfidence:P0})",
            loveVideo.Id, userId, source, status, aiResult.Decision, aiResult.Confidence);

        return new SubmitLoveVideoResponse
        {
            Success = true,
            Id = loveVideo.Id,
            Status = status,
            Message = status switch
            {
                LoveVideoStatus.Approved => "Your submission has been published!",
                LoveVideoStatus.Rejected => "Your submission couldn't be published because it doesn't meet our community guidelines.",
                _ => "Your submission is being reviewed to keep Wihngo safe and meaningful."
            }
        };
    }

    public async Task<bool> IsUrlAlreadySubmittedAsync(string youtubeUrl)
    {
        var normalizedUrl = NormalizeYoutubeUrl(youtubeUrl);
        var videoId = ExtractYoutubeVideoId(youtubeUrl);

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(SELECT 1 FROM love_videos WHERE youtube_url = @Url OR youtube_video_id = @VideoId)",
            new { Url = normalizedUrl, VideoId = videoId });
    }

    public async Task<List<LoveVideoModerationItem>> GetPendingLoveVideosAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var videos = await conn.QueryAsync<LoveVideo>(
            @"SELECT * FROM love_videos WHERE status = @PendingStatus ORDER BY created_at ASC",
            new { PendingStatus = LoveVideoStatus.Pending });

        return videos.Select(MapToModerationItem).ToList();
    }

    public async Task<LoveVideoModerationItem?> GetLoveVideoForModerationAsync(Guid loveVideoId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var video = await conn.QueryFirstOrDefaultAsync<LoveVideo>(
            "SELECT * FROM love_videos WHERE id = @Id",
            new { Id = loveVideoId });

        return video == null ? null : MapToModerationItem(video);
    }

    public async Task<LoveVideoModerationResponse> ApproveLoveVideoAsync(
        Guid loveVideoId,
        Guid moderatorUserId,
        ApproveLoveVideoRequest? request = null)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var video = await conn.QueryFirstOrDefaultAsync<LoveVideo>(
            "SELECT * FROM love_videos WHERE id = @Id",
            new { Id = loveVideoId });

        if (video == null)
        {
            return new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = loveVideoId,
                ErrorCode = LoveVideoErrorCodes.LoveVideoNotFound,
                Message = "Love video not found"
            };
        }

        if (video.Status != LoveVideoStatus.Pending)
        {
            return new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = loveVideoId,
                NewStatus = video.Status,
                ErrorCode = LoveVideoErrorCodes.NotPending,
                Message = $"Love video is not pending (current status: {video.Status})"
            };
        }

        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(
            @"UPDATE love_videos
              SET status = @Status, description = @Description,
                  moderated_by_user_id = @ModeratorId, approved_at = @ApprovedAt, updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                Status = LoveVideoStatus.Approved,
                Description = request?.Description?.Trim() ?? video.Description,
                ModeratorId = moderatorUserId,
                ApprovedAt = now,
                UpdatedAt = now,
                Id = loveVideoId
            });

        _logger.LogInformation("Love video approved: {Id} by moderator {ModeratorId}", loveVideoId, moderatorUserId);

        return new LoveVideoModerationResponse
        {
            Success = true,
            LoveVideoId = loveVideoId,
            NewStatus = LoveVideoStatus.Approved,
            Message = "Love video approved and now visible to the public"
        };
    }

    public async Task<LoveVideoModerationResponse> RejectLoveVideoAsync(
        Guid loveVideoId,
        Guid moderatorUserId,
        RejectLoveVideoRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var video = await conn.QueryFirstOrDefaultAsync<LoveVideo>(
            "SELECT * FROM love_videos WHERE id = @Id",
            new { Id = loveVideoId });

        if (video == null)
        {
            return new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = loveVideoId,
                ErrorCode = LoveVideoErrorCodes.LoveVideoNotFound,
                Message = "Love video not found"
            };
        }

        if (video.Status != LoveVideoStatus.Pending)
        {
            return new LoveVideoModerationResponse
            {
                Success = false,
                LoveVideoId = loveVideoId,
                NewStatus = video.Status,
                ErrorCode = LoveVideoErrorCodes.NotPending,
                Message = $"Love video is not pending (current status: {video.Status})"
            };
        }

        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(
            @"UPDATE love_videos
              SET status = @Status, rejection_reason = @RejectionReason,
                  moderated_by_user_id = @ModeratorId, rejected_at = @RejectedAt, updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                Status = LoveVideoStatus.Rejected,
                RejectionReason = request.Reason,
                ModeratorId = moderatorUserId,
                RejectedAt = now,
                UpdatedAt = now,
                Id = loveVideoId
            });

        _logger.LogInformation("Love video rejected: {Id} by moderator {ModeratorId}, Reason: {Reason}",
            loveVideoId, moderatorUserId, request.Reason);

        return new LoveVideoModerationResponse
        {
            Success = true,
            LoveVideoId = loveVideoId,
            NewStatus = LoveVideoStatus.Rejected,
            Message = "Love video rejected"
        };
    }

    public async Task<LoveVideoModerationResponse> HideLoveVideoAsync(
        Guid loveVideoId,
        Guid moderatorUserId,
        string reason)
    {
        return await RejectLoveVideoAsync(loveVideoId, moderatorUserId, new RejectLoveVideoRequest
        {
            Reason = $"Hidden: {reason}"
        });
    }

    public bool IsValidYoutubeUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) && YoutubeUrlRegex.IsMatch(url);
    }

    public string? ExtractYoutubeVideoId(string url)
    {
        return LoveVideo.ExtractVideoId(url);
    }

    public bool IsValidMediaKey(string mediaKey)
    {
        // Must start with love-videos/ prefix and have valid structure
        return !string.IsNullOrWhiteSpace(mediaKey) &&
               (mediaKey.StartsWith("love-videos/images/", StringComparison.OrdinalIgnoreCase) ||
                mediaKey.StartsWith("love-videos/videos/", StringComparison.OrdinalIgnoreCase));
    }

    public string GetMediaUrl(string mediaKey)
    {
        // Generate public URL for love video media
        if (string.IsNullOrWhiteSpace(_publicBucketConfig.Bucket))
        {
            // Fallback to private bucket URL pattern
            return $"https://{_awsConfig.BucketName}.s3.{_awsConfig.Region}.amazonaws.com/{mediaKey}";
        }
        return $"https://{_publicBucketConfig.Bucket}.s3.{_awsConfig.Region}.amazonaws.com/{mediaKey}";
    }

    public Task<YouTubeVideoMetadata?> FetchYoutubeMetadataAsync(string videoId)
    {
        // Placeholder - would call YouTube Data API in production
        return Task.FromResult<YouTubeVideoMetadata?>(null);
    }

    private static string GetMediaTypeFromKey(string mediaKey)
    {
        if (mediaKey.StartsWith("love-videos/images/", StringComparison.OrdinalIgnoreCase))
            return LoveVideoMediaType.Image;
        if (mediaKey.StartsWith("love-videos/videos/", StringComparison.OrdinalIgnoreCase))
            return LoveVideoMediaType.Video;
        return LoveVideoMediaType.Image; // Default fallback
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            "video/x-msvideo" => ".avi",
            _ => ".bin"
        };
    }

    private static string NormalizeYoutubeUrl(string url)
    {
        var videoId = LoveVideo.ExtractVideoId(url);
        return !string.IsNullOrEmpty(videoId)
            ? $"https://www.youtube.com/watch?v={videoId}"
            : url.Trim();
    }

    private static LoveVideoResponse MapToResponse(LoveVideo video)
    {
        // Generate YouTube thumbnail URL if we have a video ID
        string? thumbnailUrl = null;
        if (!string.IsNullOrEmpty(video.YoutubeVideoId))
        {
            thumbnailUrl = $"https://img.youtube.com/vi/{video.YoutubeVideoId}/hqdefault.jpg";
        }

        return new LoveVideoResponse
        {
            Id = video.Id,
            YoutubeUrl = video.YoutubeUrl,
            MediaUrl = video.MediaUrl,
            MediaType = video.MediaType,
            Description = video.Description,
            Status = video.Status,
            CreatedAt = video.CreatedAt,
            ApprovedAt = video.ApprovedAt,
            ThumbnailUrl = thumbnailUrl
        };
    }

    private static LoveVideoModerationItem MapToModerationItem(LoveVideo video)
    {
        return new LoveVideoModerationItem
        {
            Id = video.Id,
            YoutubeUrl = video.YoutubeUrl,
            YoutubeVideoId = video.YoutubeVideoId,
            MediaUrl = video.MediaUrl,
            MediaType = video.MediaType,
            Description = video.Description,
            Status = video.Status,
            SubmittedByUserId = video.SubmittedByUserId,
            CreatedAt = video.CreatedAt,
            RejectionReason = video.RejectionReason,
            // AI moderation fields
            AiDecision = video.AiDecision,
            AiConfidence = video.AiConfidence,
            AiFlags = ParseJsonArray(video.AiFlags),
            AiReasons = ParseJsonArray(video.AiReasons)
        };
    }

    private static List<string>? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
