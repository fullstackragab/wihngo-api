using System.ComponentModel.DataAnnotations;
using Wihngo.Models.Enums;

namespace Wihngo.Dtos;

/// <summary>
/// Request for AI story generation
/// </summary>
public class GenerateStoryRequestDto
{
    /// <summary>
    /// The bird to generate a story about (REQUIRED)
    /// </summary>
    [Required(ErrorMessage = "birdId is required")]
    public Guid BirdId { get; set; }

    /// <summary>
    /// The mood/tone of the story (OPTIONAL)
    /// </summary>
    public StoryMode? Mode { get; set; }

    /// <summary>
    /// S3 key of uploaded image for context (OPTIONAL)
    /// </summary>
    public string? ImageS3Key { get; set; }

    /// <summary>
    /// S3 key of uploaded video for context (OPTIONAL)
    /// </summary>
    public string? VideoS3Key { get; set; }

    /// <summary>
    /// Language code (e.g., "en", "es", "fr"). Default: "en"
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Story length preference (OPTIONAL). Default: Medium
    /// </summary>
    public StoryLength? Length { get; set; }
}
