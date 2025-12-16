using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for moderating user-generated content to block illegal/harmful material
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Moderate text content using OpenAI Moderation API
    /// </summary>
    Task<TextModerationResultDto> ModerateTextAsync(string text, string contentType);

    /// <summary>
    /// Moderate an image stored in S3 using AWS Rekognition
    /// </summary>
    Task<MediaModerationResultDto> ModerateImageAsync(string s3Key);

    /// <summary>
    /// Moderate a video stored in S3 using AWS Rekognition
    /// </summary>
    Task<MediaModerationResultDto> ModerateVideoAsync(string s3Key);

    /// <summary>
    /// Moderate story content (text + optional image/video) in parallel
    /// </summary>
    Task<CombinedModerationResultDto> ModerateStoryContentAsync(
        string? content,
        string? imageS3Key,
        string? videoS3Key);

    /// <summary>
    /// Moderate bird profile (name, species, tagline, description + optional image) in parallel
    /// </summary>
    Task<CombinedModerationResultDto> ModerateBirdProfileAsync(
        string? name,
        string? species,
        string? tagline,
        string? description,
        string? imageS3Key);

    /// <summary>
    /// Moderate comment text
    /// </summary>
    Task<TextModerationResultDto> ModerateCommentAsync(string text);

    /// <summary>
    /// Moderate memorial message text
    /// </summary>
    Task<TextModerationResultDto> ModerateMemorialMessageAsync(string text);
}
