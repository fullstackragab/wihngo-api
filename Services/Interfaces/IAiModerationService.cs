namespace Wihngo.Services.Interfaces;

/// <summary>
/// AI-powered content moderation service using OpenAI GPT.
/// Returns structured decisions: auto_approve, needs_human_review, reject.
/// </summary>
public interface IAiModerationService
{
    /// <summary>
    /// Evaluate a love video/story submission for moderation.
    /// </summary>
    /// <param name="request">Content to moderate</param>
    /// <returns>Structured moderation decision</returns>
    Task<AiModerationResult> ModerateContentAsync(AiModerationRequest request);
}

/// <summary>
/// Input for AI moderation
/// </summary>
public class AiModerationRequest
{
    public Guid StoryId { get; set; }
    public Guid UserId { get; set; }
    public string UserTrustLevel { get; set; } = "new"; // new, normal, trusted, verified
    public string? Text { get; set; }
    public bool HasImages { get; set; }
    public bool HasVideo { get; set; }
    public bool HasYoutubeUrl { get; set; }
    public string Language { get; set; } = "auto";
}

/// <summary>
/// AI moderation decision result
/// </summary>
public class AiModerationResult
{
    /// <summary>
    /// Decision: auto_approve, needs_human_review, reject
    /// </summary>
    public string Decision { get; set; } = "needs_human_review";

    /// <summary>
    /// Confidence score 0.0 to 1.0
    /// </summary>
    public double Confidence { get; set; } = 0.5;

    /// <summary>
    /// Human-readable reasons for the decision
    /// </summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>
    /// Machine-readable flags (e.g., spam, off_topic, violence, safe)
    /// </summary>
    public List<string> Flags { get; set; } = new();

    /// <summary>
    /// Whether AI moderation was successfully executed
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if moderation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// AI moderation decision constants
/// </summary>
public static class AiModerationDecision
{
    public const string AutoApprove = "auto_approve";
    public const string NeedsHumanReview = "needs_human_review";
    public const string Reject = "reject";
}

/// <summary>
/// User trust level constants
/// </summary>
public static class UserTrustLevel
{
    public const string New = "new";
    public const string Normal = "normal";
    public const string Trusted = "trusted";
    public const string Verified = "verified";
}
