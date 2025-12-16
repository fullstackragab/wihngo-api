using System.Text;
using System.Text.Json;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Content moderation service using OpenAI Moderation API for text and AWS Rekognition for images/videos
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly ContentModerationConfiguration _config;
    private readonly AwsConfiguration _awsConfig;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAmazonRekognition _rekognitionClient;
    private readonly IConfiguration _configuration;

    private const string OpenAiModerationUrl = "https://api.openai.com/v1/moderations";

    public ContentModerationService(
        IOptions<ContentModerationConfiguration> config,
        IOptions<AwsConfiguration> awsConfig,
        ILogger<ContentModerationService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _config = config.Value;
        _awsConfig = awsConfig.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;

        // Initialize AWS Rekognition client
        var rekognitionConfig = new AmazonRekognitionConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_awsConfig.Region)
        };

        _rekognitionClient = new AmazonRekognitionClient(
            _awsConfig.AccessKeyId,
            _awsConfig.SecretAccessKey,
            rekognitionConfig);

        _logger.LogInformation("ContentModerationService initialized. TextModeration={TextEnabled}, MediaModeration={MediaEnabled}",
            _config.EnableTextModeration, _config.EnableMediaModeration);
    }

    public async Task<TextModerationResultDto> ModerateTextAsync(string text, string contentType)
    {
        var result = new TextModerationResultDto { ContentType = contentType };

        if (!_config.EnableTextModeration || string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        try
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured, skipping text moderation");
                return result;
            }

            var requestBody = new { input = text };
            var requestJson = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiModerationUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI Moderation API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return result; // Fail open
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            var resultsArray = doc.RootElement.GetProperty("results");
            if (resultsArray.GetArrayLength() > 0)
            {
                var moderationResult = resultsArray[0];
                result.IsFlagged = moderationResult.GetProperty("flagged").GetBoolean();

                var categories = moderationResult.GetProperty("categories");
                var categoryScores = moderationResult.GetProperty("category_scores");

                foreach (var category in categories.EnumerateObject())
                {
                    var categoryName = category.Name;
                    var isFlagged = category.Value.GetBoolean();
                    var score = categoryScores.GetProperty(categoryName).GetDouble();

                    if (isFlagged || score > 0.5)
                    {
                        result.FlaggedCategories.Add(new ModerationCategoryDto
                        {
                            Category = categoryName,
                            Score = score,
                            IsFlagged = isFlagged
                        });
                    }

                    // Check if this category should block the content
                    if (isFlagged && _config.BlockedCategories.Any(bc =>
                        categoryName.Equals(bc, StringComparison.OrdinalIgnoreCase) ||
                        categoryName.StartsWith(bc.Split('/')[0], StringComparison.OrdinalIgnoreCase)))
                    {
                        result.IsBlocked = true;
                        result.BlockReason = $"Content blocked: {categoryName} detected in {contentType}";
                    }
                }
            }

            _logger.LogInformation("Text moderation completed. ContentType={ContentType}, IsFlagged={IsFlagged}, IsBlocked={IsBlocked}",
                contentType, result.IsFlagged, result.IsBlocked);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during text moderation for {ContentType}", contentType);
            return result; // Fail open
        }
    }

    public async Task<MediaModerationResultDto> ModerateImageAsync(string s3Key)
    {
        var result = new MediaModerationResultDto { MediaType = "image" };

        if (!_config.EnableMediaModeration)
        {
            _logger.LogWarning("Image moderation SKIPPED - EnableMediaModeration is false");
            return result;
        }

        if (string.IsNullOrWhiteSpace(s3Key))
        {
            _logger.LogDebug("Image moderation SKIPPED - no S3 key provided");
            return result;
        }

        _logger.LogInformation("Starting image moderation for S3Key={S3Key}, Bucket={Bucket}, Region={Region}",
            s3Key, _awsConfig.BucketName, _awsConfig.Region);

        try
        {
            var request = new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = _awsConfig.BucketName,
                        Name = s3Key
                    }
                },
                MinConfidence = _config.MinConfidencePercent
            };

            _logger.LogDebug("Calling AWS Rekognition DetectModerationLabels...");
            var response = await _rekognitionClient.DetectModerationLabelsAsync(request);
            _logger.LogWarning("Rekognition returned {LabelCount} moderation labels for {S3Key}", response.ModerationLabels.Count, s3Key);

            foreach (var label in response.ModerationLabels)
            {
                _logger.LogWarning("  -> Label: {Label}, Parent: {Parent}, Confidence: {Confidence}%",
                    label.Name, label.ParentName ?? "none", label.Confidence);

                result.DetectedLabels.Add(new ModerationLabelDto
                {
                    Label = label.Name,
                    Confidence = label.Confidence,
                    ParentLabel = label.ParentName
                });

                // Check thresholds for blocking
                var shouldBlock = ShouldBlockLabel(label);
                _logger.LogWarning("  -> ShouldBlock={ShouldBlock} for label {Label}", shouldBlock, label.Name);

                if (shouldBlock)
                {
                    result.IsFlagged = true;
                    result.IsBlocked = true;
                    result.BlockReason = $"Image blocked: {label.Name} detected ({label.Confidence:F1}% confidence)";
                }
                else if (label.Confidence >= _config.MinConfidencePercent)
                {
                    result.IsFlagged = true;
                }
            }

            _logger.LogInformation("Image moderation completed. S3Key={S3Key}, IsFlagged={IsFlagged}, IsBlocked={IsBlocked}, LabelsCount={LabelsCount}",
                s3Key, result.IsFlagged, result.IsBlocked, result.DetectedLabels.Count);

            return result;
        }
        catch (Amazon.Rekognition.AmazonRekognitionException rekEx)
        {
            _logger.LogError(rekEx, "AWS Rekognition ERROR for S3Key={S3Key}. ErrorCode={ErrorCode}, Message={Message}",
                s3Key, rekEx.ErrorCode, rekEx.Message);
            return result; // Fail open
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image moderation for S3Key={S3Key}", s3Key);
            return result; // Fail open
        }
    }

    public async Task<MediaModerationResultDto> ModerateVideoAsync(string s3Key)
    {
        var result = new MediaModerationResultDto { MediaType = "video" };

        if (!_config.EnableMediaModeration || string.IsNullOrWhiteSpace(s3Key))
        {
            return result;
        }

        try
        {
            // Start video moderation job
            var startRequest = new StartContentModerationRequest
            {
                Video = new Video
                {
                    S3Object = new S3Object
                    {
                        Bucket = _awsConfig.BucketName,
                        Name = s3Key
                    }
                },
                MinConfidence = _config.MinConfidencePercent
            };

            var startResponse = await _rekognitionClient.StartContentModerationAsync(startRequest);
            var jobId = startResponse.JobId;

            _logger.LogInformation("Started video moderation job. JobId={JobId}, S3Key={S3Key}", jobId, s3Key);

            // Wait for job to complete (with timeout)
            var maxWaitTime = TimeSpan.FromMinutes(5);
            var startTime = DateTime.UtcNow;
            GetContentModerationResponse? getResponse = null;

            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                await Task.Delay(2000); // Wait 2 seconds between checks

                var getRequest = new GetContentModerationRequest
                {
                    JobId = jobId
                };

                getResponse = await _rekognitionClient.GetContentModerationAsync(getRequest);

                if (getResponse.JobStatus == VideoJobStatus.SUCCEEDED)
                {
                    break;
                }
                else if (getResponse.JobStatus == VideoJobStatus.FAILED)
                {
                    _logger.LogError("Video moderation job failed. JobId={JobId}, StatusMessage={StatusMessage}",
                        jobId, getResponse.StatusMessage);
                    return result; // Fail open
                }
            }

            if (getResponse == null || getResponse.JobStatus != VideoJobStatus.SUCCEEDED)
            {
                _logger.LogWarning("Video moderation job timed out. JobId={JobId}", jobId);
                return result; // Fail open
            }

            // Process moderation labels from the video
            foreach (var labelDetection in getResponse.ModerationLabels)
            {
                var label = labelDetection.ModerationLabel;

                result.DetectedLabels.Add(new ModerationLabelDto
                {
                    Label = label.Name,
                    Confidence = label.Confidence,
                    ParentLabel = label.ParentName
                });

                if (ShouldBlockLabel(label))
                {
                    result.IsFlagged = true;
                    result.IsBlocked = true;
                    result.BlockReason = $"Video blocked: {label.Name} detected ({label.Confidence:F1}% confidence)";
                }
                else if (label.Confidence >= _config.MinConfidencePercent)
                {
                    result.IsFlagged = true;
                }
            }

            _logger.LogInformation("Video moderation completed. S3Key={S3Key}, IsFlagged={IsFlagged}, IsBlocked={IsBlocked}, LabelsCount={LabelsCount}",
                s3Key, result.IsFlagged, result.IsBlocked, result.DetectedLabels.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video moderation for S3Key={S3Key}", s3Key);
            return result; // Fail open
        }
    }

    public async Task<CombinedModerationResultDto> ModerateStoryContentAsync(
        string? content,
        string? imageS3Key,
        string? videoS3Key)
    {
        var result = new CombinedModerationResultDto();

        // Run text and media moderation in parallel
        var tasks = new List<Task>();

        Task<TextModerationResultDto>? textTask = null;
        Task<MediaModerationResultDto>? imageTask = null;
        Task<MediaModerationResultDto>? videoTask = null;

        if (!string.IsNullOrWhiteSpace(content))
        {
            textTask = ModerateTextAsync(content, "story_content");
            tasks.Add(textTask);
        }

        if (!string.IsNullOrWhiteSpace(imageS3Key))
        {
            imageTask = ModerateImageAsync(imageS3Key);
            tasks.Add(imageTask);
        }

        if (!string.IsNullOrWhiteSpace(videoS3Key))
        {
            videoTask = ModerateVideoAsync(videoS3Key);
            tasks.Add(videoTask);
        }

        await Task.WhenAll(tasks);

        // Collect results
        if (textTask != null)
        {
            result.TextResult = await textTask;
            if (result.TextResult.IsBlocked)
            {
                result.IsBlocked = true;
                result.Issues.Add(result.TextResult.BlockReason ?? "Text content blocked");
            }
        }

        if (imageTask != null)
        {
            result.ImageResult = await imageTask;
            if (result.ImageResult.IsBlocked)
            {
                result.IsBlocked = true;
                result.Issues.Add(result.ImageResult.BlockReason ?? "Image content blocked");
            }
        }

        if (videoTask != null)
        {
            result.VideoResult = await videoTask;
            if (result.VideoResult.IsBlocked)
            {
                result.IsBlocked = true;
                result.Issues.Add(result.VideoResult.BlockReason ?? "Video content blocked");
            }
        }

        if (result.IsBlocked)
        {
            result.BlockReason = string.Join("; ", result.Issues);
        }

        return result;
    }

    public async Task<CombinedModerationResultDto> ModerateBirdProfileAsync(
        string? name,
        string? species,
        string? tagline,
        string? description,
        string? imageS3Key)
    {
        var result = new CombinedModerationResultDto();

        // Combine all text fields for moderation
        var textParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) textParts.Add(name);
        if (!string.IsNullOrWhiteSpace(species)) textParts.Add(species);
        if (!string.IsNullOrWhiteSpace(tagline)) textParts.Add(tagline);
        if (!string.IsNullOrWhiteSpace(description)) textParts.Add(description);

        var combinedText = string.Join(" ", textParts);

        // Run text and image moderation in parallel
        var tasks = new List<Task>();

        Task<TextModerationResultDto>? textTask = null;
        Task<MediaModerationResultDto>? imageTask = null;

        if (!string.IsNullOrWhiteSpace(combinedText))
        {
            textTask = ModerateTextAsync(combinedText, "bird_profile");
            tasks.Add(textTask);
        }

        if (!string.IsNullOrWhiteSpace(imageS3Key))
        {
            imageTask = ModerateImageAsync(imageS3Key);
            tasks.Add(imageTask);
        }

        await Task.WhenAll(tasks);

        // Collect results
        if (textTask != null)
        {
            result.TextResult = await textTask;
            if (result.TextResult.IsBlocked)
            {
                result.IsBlocked = true;
                result.Issues.Add(result.TextResult.BlockReason ?? "Bird profile text blocked");
            }
        }

        if (imageTask != null)
        {
            result.ImageResult = await imageTask;
            if (result.ImageResult.IsBlocked)
            {
                result.IsBlocked = true;
                result.Issues.Add(result.ImageResult.BlockReason ?? "Bird profile image blocked");
            }
        }

        if (result.IsBlocked)
        {
            result.BlockReason = string.Join("; ", result.Issues);
        }

        return result;
    }

    public async Task<TextModerationResultDto> ModerateCommentAsync(string text)
    {
        return await ModerateTextAsync(text, "comment");
    }

    public async Task<TextModerationResultDto> ModerateMemorialMessageAsync(string text)
    {
        return await ModerateTextAsync(text, "memorial_message");
    }

    private bool ShouldBlockLabel(ModerationLabel label)
    {
        var labelName = label.Name.ToLower();
        var parentName = label.ParentName?.ToLower() ?? "";

        // AWS Rekognition NSFW labels to block
        var nsfwLabels = new[]
        {
            "explicit nudity", "nudity", "graphic male nudity", "graphic female nudity",
            "sexual activity", "illustrated explicit nudity", "adult toys",
            "female swimwear or underwear", "male swimwear or underwear",
            "partial nudity", "barechested male", "revealing clothes",
            "sexual situations", "graphic nudity"
        };

        // Check for NSFW content
        if (nsfwLabels.Any(nsfw => labelName.Contains(nsfw) || labelName == nsfw) &&
            label.Confidence >= _config.NsfwThreshold)
        {
            return true;
        }

        // Also check parent label for NSFW
        if ((parentName.Contains("nudity") || parentName.Contains("explicit") || parentName.Contains("sexual")) &&
            label.Confidence >= _config.NsfwThreshold)
        {
            return true;
        }

        // Block graphic violence
        var violenceLabels = new[]
        {
            "violence", "graphic violence", "gore", "blood", "wound",
            "weapon", "weapons", "corpse", "death", "dismemberment"
        };

        if (violenceLabels.Any(v => labelName.Contains(v)) &&
            label.Confidence >= _config.ViolenceThreshold)
        {
            return true;
        }

        if (parentName.Contains("violence") && label.Confidence >= _config.ViolenceThreshold)
        {
            return true;
        }

        // Block hate symbols
        var hateLabels = new[]
        {
            "hate", "extremist", "nazi", "confederate", "white supremacist"
        };

        if (hateLabels.Any(h => labelName.Contains(h) || parentName.Contains(h)) &&
            label.Confidence >= _config.HateThreshold)
        {
            return true;
        }

        return false;
    }
}
