using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Wihngo.Dtos;
using Wihngo.Models.Enums;
using Wihngo.Models;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for AI-powered story generation using OpenAI
/// </summary>
public class AiStoryGenerationService : IAiStoryGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AiStoryGenerationService> _logger;
    private readonly HttpClient _httpClient;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    // Rate limiting constants
    private const int MaxGenerationsPerHour = 10;
    private const int MaxGenerationsPerBirdPerHour = 5;
    private const int MaxGenerationsPerDay = 30;

    public AiStoryGenerationService(
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<AiStoryGenerationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _memoryCache = memoryCache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<GenerateStoryResponseDto> GenerateStoryAsync(GenerateStoryRequestDto request, Guid userId)
    {
        var generationId = Guid.NewGuid().ToString();

        try
        {
            // Check rate limits
            if (await IsRateLimitExceededAsync(userId, request.BirdId))
            {
                throw new InvalidOperationException("AI generation limit exceeded. Please try again later.");
            }

            // Fetch bird context
            var bird = await GetBirdContextAsync(request.BirdId, userId);
            if (bird == null)
            {
                throw new KeyNotFoundException("Bird not found");
            }

            // Build the AI prompt
            var prompt = BuildPrompt(bird, request);

            // Call OpenAI API
            var (content, tokensUsed) = await CallOpenAiAsync(prompt, request);

            // Increment rate limit counters
            await IncrementRateLimitCountersAsync(userId, request.BirdId);

            // Log analytics
            _logger.LogInformation(
                "AI story generated. GenerationId={GenerationId}, UserId={UserId}, BirdId={BirdId}, Mode={Mode}, TokensUsed={TokensUsed}",
                generationId, userId, request.BirdId, request.Mode, tokensUsed);

            return new GenerateStoryResponseDto
            {
                GeneratedContent = content,
                TokensUsed = tokensUsed,
                GenerationId = generationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate story. GenerationId={GenerationId}, UserId={UserId}", generationId, userId);
            throw;
        }
    }

    public async Task<bool> IsRateLimitExceededAsync(Guid userId, Guid birdId)
    {
        var hourlyUserKey = $"ai_gen_user_hourly_{userId}";
        var hourlyBirdKey = $"ai_gen_bird_hourly_{birdId}";
        var dailyUserKey = $"ai_gen_user_daily_{userId}";

        var hourlyUserCount = _memoryCache.Get<int>(hourlyUserKey);
        var hourlyBirdCount = _memoryCache.Get<int>(hourlyBirdKey);
        var dailyUserCount = _memoryCache.Get<int>(dailyUserKey);

        return hourlyUserCount >= MaxGenerationsPerHour ||
               hourlyBirdCount >= MaxGenerationsPerBirdPerHour ||
               dailyUserCount >= MaxGenerationsPerDay;
    }

    private async Task IncrementRateLimitCountersAsync(Guid userId, Guid birdId)
    {
        var hourlyUserKey = $"ai_gen_user_hourly_{userId}";
        var hourlyBirdKey = $"ai_gen_bird_hourly_{birdId}";
        var dailyUserKey = $"ai_gen_user_daily_{userId}";

        // Increment hourly user counter
        var hourlyUserCount = _memoryCache.Get<int>(hourlyUserKey);
        _memoryCache.Set(hourlyUserKey, hourlyUserCount + 1, TimeSpan.FromHours(1));

        // Increment hourly bird counter
        var hourlyBirdCount = _memoryCache.Get<int>(hourlyBirdKey);
        _memoryCache.Set(hourlyBirdKey, hourlyBirdCount + 1, TimeSpan.FromHours(1));

        // Increment daily user counter
        var dailyUserCount = _memoryCache.Get<int>(dailyUserKey);
        _memoryCache.Set(dailyUserKey, dailyUserCount + 1, TimeSpan.FromDays(1));

        await Task.CompletedTask;
    }

    private async Task<Bird?> GetBirdContextAsync(Guid birdId, Guid userId)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new NpgsqlConnection(connectionString);

        const string sql = @"
            SELECT bird_id, owner_id, name, species, tagline, description,
                   is_memorial, created_at
            FROM birds
            WHERE bird_id = @BirdId AND owner_id = @UserId";

        return await connection.QueryFirstOrDefaultAsync<Bird>(sql, new { BirdId = birdId, UserId = userId });
    }

    private string BuildPrompt(Bird bird, GenerateStoryRequestDto request)
    {
        var moodGuidelines = GetMoodGuidelines(request.Mode);
        var moodName = request.Mode?.ToString() ?? "DailyLife";
        var (minWords, maxWords) = GetWordCountRange(request.Length);

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are helping a bird owner write a heartfelt story about their beloved bird for a community platform called Wihngo.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("BIRD INFORMATION:");
        promptBuilder.AppendLine($"- Name: {bird.Name}");
        promptBuilder.AppendLine($"- Species: {bird.Species}");

        if (!string.IsNullOrEmpty(bird.Tagline))
            promptBuilder.AppendLine($"- Tagline: {bird.Tagline}");

        if (!string.IsNullOrEmpty(bird.Description))
            promptBuilder.AppendLine($"- Description: {bird.Description}");

        promptBuilder.AppendLine($"- Is Memorial: {(bird.IsMemorial ? "Yes, this bird has passed away" : "No")}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine($"STORY MOOD: {moodName}");
        promptBuilder.AppendLine($"Mood guidelines: {moodGuidelines}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("INSTRUCTIONS:");
        promptBuilder.AppendLine("1. Write a story in first person from the owner's perspective");
        promptBuilder.AppendLine("2. Make it authentic, heartfelt, and engaging");
        promptBuilder.AppendLine($"3. Naturally include the bird's name ({bird.Name})");
        promptBuilder.AppendLine("4. Match the tone to the selected mood");
        promptBuilder.AppendLine($"5. Keep it between {minWords}-{maxWords} words");
        promptBuilder.AppendLine("6. Make it feel like a real story shared by a bird lover");
        promptBuilder.AppendLine("7. DO NOT include a title or heading - start directly with the story content");

        if (bird.IsMemorial)
            promptBuilder.AppendLine("8. This is a memorial bird - be respectful and focus on cherished memories");

        promptBuilder.AppendLine($"{(bird.IsMemorial ? "9" : "8")}. Write in {GetLanguageName(request.Language)}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate the story content only (no title):");

        return promptBuilder.ToString();
    }

    private string GetMoodGuidelines(StoryMode? mode)
    {
        return mode switch
        {
            StoryMode.LoveAndBond => "Warm, affectionate, emotional. Focus on connection, trust, cuddles",
            StoryMode.NewBeginning => "Hopeful, excited, welcoming. Focus on first moments, new journey",
            StoryMode.ProgressAndWins => "Celebratory, proud, encouraging. Focus on achievements, milestones",
            StoryMode.FunnyMoment => "Playful, humorous, light-hearted. Focus on quirks, silly behavior",
            StoryMode.PeacefulMoment => "Calm, serene, reflective. Focus on quiet beauty, contentment",
            StoryMode.LossAndMemory => "Gentle, respectful, nostalgic. Focus on memories, tribute (handle sensitively)",
            StoryMode.CareAndHealth => "Informative, caring, reassuring. Focus on health updates, recovery",
            StoryMode.DailyLife => "Casual, relatable, everyday. Focus on routines, simple joys",
            _ => "Casual, relatable, everyday. Focus on routines, simple joys"
        };
    }

    private string GetLanguageName(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "en" => "English",
            "es" => "Spanish",
            "fr" => "French",
            "de" => "German",
            "it" => "Italian",
            "pt" => "Portuguese",
            "ja" => "Japanese",
            "zh" => "Chinese",
            "ar" => "Arabic",
            "hi" => "Hindi",
            "id" => "Indonesian",
            "vi" => "Vietnamese",
            "th" => "Thai",
            "ko" => "Korean",
            "tr" => "Turkish",
            "pl" => "Polish",
            _ => "English"
        };
    }

    private (int minWords, int maxWords) GetWordCountRange(StoryLength? length)
    {
        return length switch
        {
            StoryLength.Short => (50, 150),
            StoryLength.Medium => (150, 300),
            _ => (50, 150) // Default to Short
        };
    }

    private async Task<(string content, int tokensUsed)> CallOpenAiAsync(string prompt, GenerateStoryRequestDto request)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured");
        }

        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a creative writer helping bird owners share their stories." },
                new { role = "user", content = prompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiApiUrl)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"OpenAI API returned {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var tokensUsed = doc.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        // Ensure content doesn't exceed 5000 characters
        if (content.Length > 5000)
        {
            content = content.Substring(0, 5000);
        }

        return (content, tokensUsed);
    }
}
