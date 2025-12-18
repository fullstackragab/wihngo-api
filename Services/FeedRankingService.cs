using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service for ranking and personalizing feed content based on user preferences and interactions.
    /// </summary>
    public class FeedRankingService : IFeedRankingService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserPreferencesService _preferencesService;
        private readonly IBirdFollowService _birdFollowService;
        private readonly IS3Service _s3Service;
        private readonly ILogger<FeedRankingService> _logger;

        // Scoring weights
        private const double SameLanguageWeight = 40;
        private const double SameCountryWeight = 25;
        private const double FollowedBirdWeight = 30;
        private const double RecencyWeight = 15;
        private const double TrendingWeight = 10;
        private const int RecencyDays = 7;
        private const int TrendingLikesThreshold = 10;
        private const int TrendingCommentsThreshold = 5;

        public FeedRankingService(
            IDbConnectionFactory dbFactory,
            IUserPreferencesService preferencesService,
            IBirdFollowService birdFollowService,
            IS3Service s3Service,
            ILogger<FeedRankingService> logger)
        {
            _dbFactory = dbFactory;
            _preferencesService = preferencesService;
            _birdFollowService = birdFollowService;
            _s3Service = s3Service;
            _logger = logger;
        }

        public async Task<PagedResult<RankedStoryDto>> GetRankedFeedAsync(Guid? userId, FeedRequestDto request)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get user context for personalization
            var userPreferences = userId.HasValue
                ? await _preferencesService.GetUserPreferencesAsync(userId.Value)
                : null;

            var followedBirdIds = userId.HasValue
                ? await _birdFollowService.GetFollowedBirdIdsAsync(userId.Value)
                : new List<Guid>();

            // Apply filters from request or user preferences
            var preferredLanguages = !string.IsNullOrEmpty(request.Language)
                ? new List<string> { request.Language }
                : userPreferences?.PreferredLanguages ?? new List<string>();

            var preferredCountry = !string.IsNullOrEmpty(request.Country)
                ? request.Country
                : userPreferences?.Country;

            // Build query with ranking
            var sql = @"
                SELECT
                    s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                    s.language, s.country, s.like_count, s.comment_count,
                    b.name as bird_name, b.bird_id
                FROM stories s
                JOIN birds b ON s.bird_id = b.bird_id
                WHERE 1=1";

            var parameters = new DynamicParameters();

            // Apply mode filter if specified
            if (request.Mode.HasValue)
            {
                sql += " AND s.mode = @Mode";
                parameters.Add("Mode", (int)request.Mode.Value);
            }

            sql += " ORDER BY s.created_at DESC LIMIT @Limit OFFSET @Offset";
            parameters.Add("Limit", request.PageSize * 3); // Fetch more for ranking
            parameters.Add("Offset", 0);

            var stories = await connection.QueryAsync<dynamic>(sql, parameters);
            var storyList = stories.ToList();

            // Calculate scores and rank
            var rankedStories = new List<(RankedStoryDto dto, double score)>();

            foreach (var story in storyList)
            {
                var dto = await MapToRankedStoryDto(story);
                var score = CalculateScore(dto, preferredLanguages, preferredCountry, followedBirdIds);
                dto.RelevanceScore = score;
                dto.MatchReason = DetermineMatchReason(dto, preferredLanguages, preferredCountry, followedBirdIds);
                rankedStories.Add((dto, score));
            }

            // Sort by score and paginate
            var sortedStories = rankedStories
                .OrderByDescending(x => x.score)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.dto)
                .ToList();

            // Get total count
            var countSql = "SELECT COUNT(*) FROM stories";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            return new PagedResult<RankedStoryDto>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Items = sortedStories
            };
        }

        public async Task<FeedSectionDto> GetFeedSectionAsync(Guid? userId, string sectionType, int limit = 10)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var userPreferences = userId.HasValue
                ? await _preferencesService.GetUserPreferencesAsync(userId.Value)
                : null;

            var followedBirdIds = userId.HasValue
                ? await _birdFollowService.GetFollowedBirdIdsAsync(userId.Value)
                : new List<Guid>();

            string sql;
            var parameters = new DynamicParameters();
            parameters.Add("Limit", limit);

            switch (sectionType.ToLowerInvariant())
            {
                case "from_your_area":
                    if (string.IsNullOrEmpty(userPreferences?.Country))
                    {
                        return new FeedSectionDto
                        {
                            SectionType = sectionType,
                            Title = "From Your Area",
                            Stories = new List<RankedStoryDto>(),
                            HasMore = false
                        };
                    }
                    sql = @"
                        SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                               s.language, s.country, s.like_count, s.comment_count, b.name as bird_name, b.bird_id
                        FROM stories s
                        JOIN birds b ON s.bird_id = b.bird_id
                        WHERE s.country = @Country
                        ORDER BY s.created_at DESC
                        LIMIT @Limit";
                    parameters.Add("Country", userPreferences.Country);
                    break;

                case "in_your_language":
                    var languages = userPreferences?.PreferredLanguages ?? new List<string>();
                    if (!languages.Any())
                    {
                        languages = new List<string> { "en" }; // Default to English
                    }
                    sql = @"
                        SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                               s.language, s.country, s.like_count, s.comment_count, b.name as bird_name, b.bird_id
                        FROM stories s
                        JOIN birds b ON s.bird_id = b.bird_id
                        WHERE s.language = ANY(@Languages)
                        ORDER BY s.created_at DESC
                        LIMIT @Limit";
                    parameters.Add("Languages", languages.ToArray());
                    break;

                case "discover_worldwide":
                    sql = @"
                        SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                               s.language, s.country, s.like_count, s.comment_count, b.name as bird_name, b.bird_id
                        FROM stories s
                        JOIN birds b ON s.bird_id = b.bird_id
                        WHERE s.like_count >= @LikesThreshold OR s.comment_count >= @CommentsThreshold
                        ORDER BY (s.like_count + s.comment_count * 2) DESC, s.created_at DESC
                        LIMIT @Limit";
                    parameters.Add("LikesThreshold", TrendingLikesThreshold);
                    parameters.Add("CommentsThreshold", TrendingCommentsThreshold);
                    break;

                case "followed_birds":
                    if (!followedBirdIds.Any())
                    {
                        return new FeedSectionDto
                        {
                            SectionType = sectionType,
                            Title = "Birds You Follow",
                            Stories = new List<RankedStoryDto>(),
                            HasMore = false
                        };
                    }
                    sql = @"
                        SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                               s.language, s.country, s.like_count, s.comment_count, b.name as bird_name, b.bird_id
                        FROM stories s
                        JOIN birds b ON s.bird_id = b.bird_id
                        WHERE s.bird_id = ANY(@BirdIds)
                        ORDER BY s.created_at DESC
                        LIMIT @Limit";
                    parameters.Add("BirdIds", followedBirdIds.ToArray());
                    break;

                default:
                    throw new ArgumentException($"Unknown section type: {sectionType}");
            }

            var stories = await connection.QueryAsync<dynamic>(sql, parameters);
            var storyDtos = new List<RankedStoryDto>();

            foreach (var story in stories)
            {
                var dto = await MapToRankedStoryDto(story);
                dto.MatchReason = sectionType;
                storyDtos.Add(dto);
            }

            return new FeedSectionDto
            {
                SectionType = sectionType,
                Title = GetSectionTitle(sectionType),
                Stories = storyDtos,
                HasMore = storyDtos.Count >= limit
            };
        }

        public async Task<List<FeedSectionDto>> GetAllFeedSectionsAsync(Guid? userId, int storiesPerSection = 5)
        {
            var sections = new List<FeedSectionDto>();

            // Get user preferences to determine which sections to show
            var userPreferences = userId.HasValue
                ? await _preferencesService.GetUserPreferencesAsync(userId.Value)
                : null;

            var followedBirdIds = userId.HasValue
                ? await _birdFollowService.GetFollowedBirdIdsAsync(userId.Value)
                : new List<Guid>();

            // Followed birds section (if user follows any)
            if (followedBirdIds.Any())
            {
                var followedSection = await GetFeedSectionAsync(userId, "followed_birds", storiesPerSection);
                if (followedSection.Stories.Any())
                {
                    sections.Add(followedSection);
                }
            }

            // In your language section
            var languageSection = await GetFeedSectionAsync(userId, "in_your_language", storiesPerSection);
            if (languageSection.Stories.Any())
            {
                sections.Add(languageSection);
            }

            // From your area section (if user has country set)
            if (!string.IsNullOrEmpty(userPreferences?.Country))
            {
                var areaSection = await GetFeedSectionAsync(userId, "from_your_area", storiesPerSection);
                if (areaSection.Stories.Any())
                {
                    sections.Add(areaSection);
                }
            }

            // Discover worldwide (trending)
            var trendingSection = await GetFeedSectionAsync(userId, "discover_worldwide", storiesPerSection);
            sections.Add(trendingSection);

            return sections;
        }

        public async Task<List<RankedStoryDto>> GetTrendingStoriesAsync(int limit = 10)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = @"
                SELECT s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                       s.language, s.country, s.like_count, s.comment_count, b.name as bird_name, b.bird_id
                FROM stories s
                JOIN birds b ON s.bird_id = b.bird_id
                WHERE s.created_at > @Since
                ORDER BY (s.like_count + s.comment_count * 2) DESC
                LIMIT @Limit";

            var stories = await connection.QueryAsync<dynamic>(sql, new
            {
                Since = DateTime.UtcNow.AddDays(-7),
                Limit = limit
            });

            var result = new List<RankedStoryDto>();
            foreach (var story in stories)
            {
                var dto = await MapToRankedStoryDto(story);
                dto.MatchReason = "trending";
                result.Add(dto);
            }

            return result;
        }

        private double CalculateScore(
            RankedStoryDto story,
            List<string> preferredLanguages,
            string? preferredCountry,
            List<Guid> followedBirdIds)
        {
            double score = 0;

            // Same language bonus
            if (!string.IsNullOrEmpty(story.Language) &&
                preferredLanguages.Any(l => l.Equals(story.Language, StringComparison.OrdinalIgnoreCase)))
            {
                score += SameLanguageWeight;
            }

            // Same country bonus
            if (!string.IsNullOrEmpty(story.Country) &&
                story.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
            {
                score += SameCountryWeight;
            }

            // Followed bird bonus (need to get bird ID from story - simplified here)
            // In a real implementation, we'd need to track the bird ID
            if (followedBirdIds.Any())
            {
                score += FollowedBirdWeight * 0.5; // Partial bonus for having followed birds
            }

            // Recency bonus (linear decay over 7 days)
            if (story.CreatedAt.HasValue)
            {
                var ageInDays = (DateTime.UtcNow - story.CreatedAt.Value).TotalDays;
                if (ageInDays <= RecencyDays)
                {
                    score += RecencyWeight * (1 - ageInDays / RecencyDays);
                }
            }

            // Trending bonus
            if (story.LikeCount >= TrendingLikesThreshold || story.CommentCount >= TrendingCommentsThreshold)
            {
                score += TrendingWeight;
            }

            return score;
        }

        private string? DetermineMatchReason(
            RankedStoryDto story,
            List<string> preferredLanguages,
            string? preferredCountry,
            List<Guid> followedBirdIds)
        {
            if (!string.IsNullOrEmpty(story.Language) &&
                preferredLanguages.Any(l => l.Equals(story.Language, StringComparison.OrdinalIgnoreCase)))
            {
                return "in_your_language";
            }

            if (!string.IsNullOrEmpty(story.Country) &&
                story.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
            {
                return "from_your_area";
            }

            if (story.LikeCount >= TrendingLikesThreshold || story.CommentCount >= TrendingCommentsThreshold)
            {
                return "trending";
            }

            return null;
        }

        private async Task<RankedStoryDto> MapToRankedStoryDto(dynamic story)
        {
            string? imageUrl = null;
            string? videoUrl = null;

            if (!string.IsNullOrWhiteSpace((string?)story.image_url))
            {
                try
                {
                    imageUrl = await _s3Service.GenerateDownloadUrlAsync(story.image_url);
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace((string?)story.video_url))
            {
                try
                {
                    videoUrl = await _s3Service.GenerateDownloadUrlAsync(story.video_url);
                }
                catch { }
            }

            string content = story.content ?? string.Empty;
            string birdName = story.bird_name ?? string.Empty;

            return new RankedStoryDto
            {
                StoryId = (Guid)story.story_id,
                Birds = string.IsNullOrWhiteSpace(birdName) ? new List<string>() : new List<string> { birdName },
                Mode = (StoryMode?)story.mode,
                Date = ((DateTime)story.created_at).ToString("MMMM d, yyyy"),
                Preview = content.Length > 140 ? content.Substring(0, 140) + "..." : content,
                ImageS3Key = story.image_url,
                ImageUrl = imageUrl,
                VideoS3Key = story.video_url,
                VideoUrl = videoUrl,
                Language = story.language,
                Country = story.country,
                LikeCount = (int?)story.like_count ?? 0,
                CommentCount = (int?)story.comment_count ?? 0,
                CreatedAt = (DateTime?)story.created_at
            };
        }

        private static string GetSectionTitle(string sectionType)
        {
            return sectionType.ToLowerInvariant() switch
            {
                "from_your_area" => "From Your Area",
                "in_your_language" => "In Your Language",
                "discover_worldwide" => "Discover Worldwide",
                "followed_birds" => "Birds You Follow",
                _ => sectionType
            };
        }
    }
}
