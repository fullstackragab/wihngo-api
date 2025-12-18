using Dapper;
using Wihngo.Data;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service to backfill language detection for existing stories.
    /// Can be run as a one-time migration or periodically to fill gaps.
    /// </summary>
    public class StoryLanguageBackfillService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILanguageDetectionService _languageDetectionService;
        private readonly ILogger<StoryLanguageBackfillService> _logger;
        private const int BatchSize = 100;

        public StoryLanguageBackfillService(
            IDbConnectionFactory dbFactory,
            ILanguageDetectionService languageDetectionService,
            ILogger<StoryLanguageBackfillService> logger)
        {
            _dbFactory = dbFactory;
            _languageDetectionService = languageDetectionService;
            _logger = logger;
        }

        /// <summary>
        /// Backfill language detection for all stories that don't have a language set.
        /// Processes in batches to avoid memory issues.
        /// </summary>
        /// <returns>Number of stories updated</returns>
        public async Task<BackfillResult> BackfillStoriesAsync(CancellationToken cancellationToken = default)
        {
            var result = new BackfillResult();

            _logger.LogInformation("Starting story language backfill...");

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get total count of stories needing backfill
            var totalToProcess = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM stories WHERE language IS NULL");

            _logger.LogInformation("Found {Count} stories without language detection", totalToProcess);
            result.TotalToProcess = totalToProcess;

            if (totalToProcess == 0)
            {
                _logger.LogInformation("No stories need language backfill");
                return result;
            }

            var processed = 0;
            var offset = 0;

            while (processed < totalToProcess && !cancellationToken.IsCancellationRequested)
            {
                // Fetch batch of stories
                var sql = @"
                    SELECT story_id, content, author_id
                    FROM stories
                    WHERE language IS NULL
                    ORDER BY created_at DESC
                    OFFSET @Offset LIMIT @Limit";

                var stories = await connection.QueryAsync<dynamic>(sql, new { Offset = offset, Limit = BatchSize });
                var storyList = stories.ToList();

                if (!storyList.Any())
                    break;

                foreach (var story in storyList)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        Guid storyId = (Guid)story.story_id;
                        string content = story.content ?? string.Empty;
                        Guid authorId = (Guid)story.author_id;

                        // Detect language
                        var language = _languageDetectionService.DetectLanguage(content);

                        // Get author's country
                        var countrySql = "SELECT country FROM users WHERE user_id = @UserId";
                        var country = await connection.QueryFirstOrDefaultAsync<string?>(countrySql, new { UserId = authorId });

                        // Update story
                        var updateSql = @"
                            UPDATE stories
                            SET language = @Language, country = @Country
                            WHERE story_id = @StoryId";

                        await connection.ExecuteAsync(updateSql, new
                        {
                            Language = language,
                            Country = country,
                            StoryId = storyId
                        });

                        if (language != null)
                        {
                            result.LanguagesDetected[language] = result.LanguagesDetected.GetValueOrDefault(language) + 1;
                        }
                        else
                        {
                            result.FailedDetections++;
                        }

                        processed++;
                        result.Processed++;
                    }
                    catch (Exception ex)
                    {
                        var storyIdForLog = (Guid)story.story_id;
                        _logger.LogWarning(ex, "Failed to backfill language for story {StoryId}", storyIdForLog);
                        result.Errors++;
                        processed++; // Still count it as processed to avoid infinite loop
                    }
                }

                offset += BatchSize;

                // Log progress every batch
                _logger.LogInformation("Backfill progress: {Processed}/{Total} stories processed",
                    processed, totalToProcess);

                // Small delay between batches to reduce database load
                await Task.Delay(100, cancellationToken);
            }

            _logger.LogInformation("Story language backfill completed. Processed: {Processed}, Errors: {Errors}",
                result.Processed, result.Errors);

            return result;
        }

        /// <summary>
        /// Backfill country for all stories based on their author's country.
        /// Only updates stories where country is null but author has a country set.
        /// </summary>
        public async Task<int> BackfillCountriesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting story country backfill...");

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Update stories with author's country where story country is null
            var updateSql = @"
                UPDATE stories s
                SET country = u.country
                FROM users u
                WHERE s.author_id = u.user_id
                  AND s.country IS NULL
                  AND u.country IS NOT NULL";

            var updated = await connection.ExecuteAsync(updateSql);

            _logger.LogInformation("Story country backfill completed. Updated {Count} stories", updated);

            return updated;
        }
    }

    /// <summary>
    /// Result of a language backfill operation.
    /// </summary>
    public class BackfillResult
    {
        public int TotalToProcess { get; set; }
        public int Processed { get; set; }
        public int Errors { get; set; }
        public int FailedDetections { get; set; }
        public Dictionary<string, int> LanguagesDetected { get; set; } = new();
    }
}
