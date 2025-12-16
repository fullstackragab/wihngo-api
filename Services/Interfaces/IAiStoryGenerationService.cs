using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for AI-powered story generation
/// </summary>
public interface IAiStoryGenerationService
{
    /// <summary>
    /// Generate a story based on bird context, mood, and optional media
    /// </summary>
    Task<GenerateStoryResponseDto> GenerateStoryAsync(GenerateStoryRequestDto request, Guid userId);

    /// <summary>
    /// Check if user has exceeded rate limits for AI generation
    /// </summary>
    Task<bool> IsRateLimitExceededAsync(Guid userId, Guid birdId);
}
