namespace Wihngo.Dtos;

/// <summary>
/// Result of text moderation using OpenAI Moderation API
/// </summary>
public class TextModerationResultDto
{
    public bool IsFlagged { get; set; }
    public bool IsBlocked { get; set; }
    public List<ModerationCategoryDto> FlaggedCategories { get; set; } = new();
    public string? BlockReason { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Result of media moderation using AWS Rekognition
/// </summary>
public class MediaModerationResultDto
{
    public bool IsFlagged { get; set; }
    public bool IsBlocked { get; set; }
    public List<ModerationLabelDto> DetectedLabels { get; set; } = new();
    public string? BlockReason { get; set; }
    public string MediaType { get; set; } = string.Empty;
}

/// <summary>
/// Combined result of text and media moderation
/// </summary>
public class CombinedModerationResultDto
{
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public TextModerationResultDto? TextResult { get; set; }
    public MediaModerationResultDto? ImageResult { get; set; }
    public MediaModerationResultDto? VideoResult { get; set; }

    /// <summary>
    /// List of all issues found during moderation
    /// </summary>
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Category flagged by OpenAI Moderation API
/// </summary>
public class ModerationCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool IsFlagged { get; set; }
}

/// <summary>
/// Label detected by AWS Rekognition
/// </summary>
public class ModerationLabelDto
{
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? ParentLabel { get; set; }
}
