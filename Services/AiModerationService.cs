using System.Text;
using System.Text.Json;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// AI-powered content moderation using OpenAI GPT.
/// Classifies content and returns structured decisions without interpretation.
/// </summary>
public class AiModerationService : IAiModerationService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiModerationService> _logger;

    private const string OpenAiChatUrl = "https://api.openai.com/v1/chat/completions";

    public AiModerationService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AiModerationService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<AiModerationResult> ModerateContentAsync(AiModerationRequest request)
    {
        var defaultResult = new AiModerationResult
        {
            Decision = AiModerationDecision.NeedsHumanReview,
            Confidence = 0.5,
            Reasons = new List<string> { "AI moderation unavailable, defaulting to human review" },
            Flags = new List<string> { "fallback" }
        };

        try
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured, defaulting to human review");
                return defaultResult;
            }

            // Skip AI moderation if no text content
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                // Media-only submission - needs human review
                return new AiModerationResult
                {
                    Decision = AiModerationDecision.NeedsHumanReview,
                    Confidence = 0.6,
                    Reasons = new List<string> { "Media-only submission requires human review" },
                    Flags = new List<string> { "media_only" }
                };
            }

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request);

            var requestBody = new
            {
                model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1, // Low temperature for consistent decisions
                max_tokens = 500,
                response_format = new { type = "json_object" }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiChatUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                defaultResult.Error = $"OpenAI API error: {response.StatusCode}";
                return defaultResult;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = ParseModerationResponse(responseContent, request.UserTrustLevel);

            _logger.LogInformation(
                "AI moderation completed for story {StoryId}: Decision={Decision}, Confidence={Confidence}, Flags={Flags}",
                request.StoryId, result.Decision, result.Confidence, string.Join(",", result.Flags));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI moderation for story {StoryId}", request.StoryId);
            defaultResult.Error = ex.Message;
            defaultResult.Success = false;
            return defaultResult;
        }
    }

    private static string BuildSystemPrompt()
    {
        return @"You are a content moderation AI for Wihngo, a platform about bird care, rescue, love, and support.

Your job is to evaluate user submissions and return a structured moderation decision.

## EVALUATION CRITERIA (in order of priority)

1. SAFETY - Check for:
   - Hate speech
   - Violence or graphic content
   - Sexual/NSFW content
   - Harassment
   - Exploitation

2. SPAM/SCAM - Check for:
   - Advertisements
   - Crypto promotions
   - External selling
   - Repetitive/copied content
   - Suspicious links

3. RELEVANCE - Check for:
   - Related to birds, pets, or animal care
   - Ethical animal treatment
   - Love, care, rescue, support themes

4. QUALITY - Check for:
   - Meaningful content
   - Not extremely low effort
   - Not misleading

## DECISION RULES

- Clear violation → ""reject""
- Uncertain or borderline → ""needs_human_review""
- Safe + relevant → ""auto_approve""

## TRUST LEVEL MODIFIER

- ""trusted"" or ""verified"" users → be MORE permissive
- ""new"" users → be MORE conservative

## OUTPUT FORMAT

You MUST respond ONLY with valid JSON:

{
  ""decision"": ""auto_approve"" | ""needs_human_review"" | ""reject"",
  ""confidence"": 0.0-1.0,
  ""reasons"": [""short reason 1"", ""short reason 2""],
  ""flags"": [""safe"" | ""spam"" | ""off_topic"" | ""violence"" | ""nsfw"" | ""hate"" | ""low_quality"" | ""promotional""]
}

## CONSTRAINTS

- NO explanations outside JSON
- NO markdown
- NO commentary
- NO policy mentions";
    }

    private static string BuildUserPrompt(AiModerationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Story ID: {request.StoryId}");
        sb.AppendLine($"User Trust Level: {request.UserTrustLevel}");
        sb.AppendLine($"Has Images: {request.HasImages}");
        sb.AppendLine($"Has Video: {request.HasVideo}");
        sb.AppendLine($"Has YouTube URL: {request.HasYoutubeUrl}");
        sb.AppendLine($"Language: {request.Language}");
        sb.AppendLine();
        sb.AppendLine("Content:");
        sb.AppendLine(request.Text ?? "(no text content)");

        return sb.ToString();
    }

    private AiModerationResult ParseModerationResponse(string responseJson, string userTrustLevel)
    {
        var defaultResult = new AiModerationResult
        {
            Decision = AiModerationDecision.NeedsHumanReview,
            Confidence = 0.5,
            Reasons = new List<string> { "Failed to parse AI response" },
            Flags = new List<string> { "parse_error" }
        };

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Extract the message content
            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return defaultResult;
            }

            var messageContent = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return defaultResult;
            }

            // Parse the JSON response from GPT
            using var resultDoc = JsonDocument.Parse(messageContent);
            var resultRoot = resultDoc.RootElement;

            var decision = resultRoot.TryGetProperty("decision", out var decProp)
                ? decProp.GetString() ?? AiModerationDecision.NeedsHumanReview
                : AiModerationDecision.NeedsHumanReview;

            var confidence = resultRoot.TryGetProperty("confidence", out var confProp)
                ? confProp.GetDouble()
                : 0.5;

            var reasons = new List<string>();
            if (resultRoot.TryGetProperty("reasons", out var reasonsProp) && reasonsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in reasonsProp.EnumerateArray())
                {
                    var reason = r.GetString();
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        reasons.Add(reason);
                    }
                }
            }

            var flags = new List<string>();
            if (resultRoot.TryGetProperty("flags", out var flagsProp) && flagsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in flagsProp.EnumerateArray())
                {
                    var flag = f.GetString();
                    if (!string.IsNullOrWhiteSpace(flag))
                    {
                        flags.Add(flag);
                    }
                }
            }

            // Validate decision value
            if (decision != AiModerationDecision.AutoApprove &&
                decision != AiModerationDecision.NeedsHumanReview &&
                decision != AiModerationDecision.Reject)
            {
                decision = AiModerationDecision.NeedsHumanReview;
            }

            // Apply trust level adjustments
            var adjustedResult = ApplyTrustLevelAdjustments(decision, confidence, userTrustLevel);

            return new AiModerationResult
            {
                Decision = adjustedResult.decision,
                Confidence = Math.Clamp(adjustedResult.confidence, 0.0, 1.0),
                Reasons = reasons.Count > 0 ? reasons : new List<string> { "AI evaluation complete" },
                Flags = flags.Count > 0 ? flags : new List<string> { "evaluated" },
                Success = true
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI moderation response JSON");
            return defaultResult;
        }
    }

    private static (string decision, double confidence) ApplyTrustLevelAdjustments(
        string decision, double confidence, string trustLevel)
    {
        // For trusted/verified users, be more permissive
        if (trustLevel is UserTrustLevel.Trusted or UserTrustLevel.Verified)
        {
            // If needs_human_review with high confidence, consider auto_approve
            if (decision == AiModerationDecision.NeedsHumanReview && confidence >= 0.7)
            {
                return (AiModerationDecision.AutoApprove, confidence);
            }
        }

        // For new users, be more conservative
        if (trustLevel == UserTrustLevel.New)
        {
            // If auto_approve with lower confidence, require human review
            if (decision == AiModerationDecision.AutoApprove && confidence < 0.85)
            {
                return (AiModerationDecision.NeedsHumanReview, confidence);
            }
        }

        return (decision, confidence);
    }
}
