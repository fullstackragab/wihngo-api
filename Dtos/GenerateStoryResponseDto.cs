namespace Wihngo.Dtos;

/// <summary>
/// Response from AI story generation
/// </summary>
public class GenerateStoryResponseDto
{
    /// <summary>
    /// The AI-generated story content (max 5000 chars)
    /// </summary>
    public string GeneratedContent { get; set; } = string.Empty;

    /// <summary>
    /// Number of AI tokens consumed (OPTIONAL)
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Unique ID for tracking/analytics (OPTIONAL)
    /// </summary>
    public string? GenerationId { get; set; }
}
