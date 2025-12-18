using System.ComponentModel.DataAnnotations;
using Wihngo.Models.Enums;

namespace Wihngo.Dtos
{
    /// <summary>
    /// Story with ranking information for personalized feed.
    /// </summary>
    public class RankedStoryDto : StorySummaryDto
    {
        /// <summary>
        /// Relevance score calculated by the ranking algorithm.
        /// Higher scores appear first in the feed.
        /// </summary>
        public double RelevanceScore { get; set; }

        /// <summary>
        /// ISO 639-1 language code of the story content.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 country code from story author.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Reason this story matched the user's preferences.
        /// Examples: "same_language", "from_your_area", "followed_bird", "trending"
        /// </summary>
        public string? MatchReason { get; set; }
    }

    /// <summary>
    /// A section of the feed (e.g., "From Your Area", "In Your Language").
    /// </summary>
    public class FeedSectionDto
    {
        /// <summary>
        /// Section type identifier.
        /// Values: "from_your_area", "in_your_language", "discover_worldwide", "followed_birds"
        /// </summary>
        public string SectionType { get; set; } = string.Empty;

        /// <summary>
        /// Localized title to display (e.g., "From Your Area").
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Stories in this section.
        /// </summary>
        public List<RankedStoryDto> Stories { get; set; } = new();

        /// <summary>
        /// Whether there are more stories to load in this section.
        /// </summary>
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// User's feed and content preferences.
    /// </summary>
    public class UserPreferencesDto
    {
        /// <summary>
        /// List of preferred content language codes (ISO 639-1).
        /// Empty list means show all languages.
        /// </summary>
        public List<string> PreferredLanguages { get; set; } = new();

        /// <summary>
        /// User's country code (ISO 3166-1 alpha-2).
        /// </summary>
        public string? Country { get; set; }
    }

    /// <summary>
    /// Request to update preferred languages.
    /// </summary>
    public class UpdateLanguagesDto
    {
        /// <summary>
        /// List of ISO 639-1 language codes.
        /// Valid values: ar, de, en, es, fr, hi, id, it, ja, ko, pl, pt, th, tr, vi, zh
        /// </summary>
        [Required]
        public List<string> Languages { get; set; } = new();
    }

    /// <summary>
    /// Request to update user's country.
    /// </summary>
    public class UpdateCountryDto
    {
        /// <summary>
        /// ISO 3166-1 alpha-2 country code (e.g., "US", "SA", "JP").
        /// </summary>
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to record a feed interaction.
    /// </summary>
    public class RecordInteractionDto
    {
        /// <summary>
        /// Type of interaction: "view", "skip", "share"
        /// Note: "like" and "comment" are tracked via their own endpoints.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string InteractionType { get; set; } = string.Empty;

        /// <summary>
        /// Time spent viewing in seconds (optional, for "view" interactions).
        /// </summary>
        public int? DurationSeconds { get; set; }
    }

    /// <summary>
    /// Feed request parameters.
    /// </summary>
    public class FeedRequestDto
    {
        /// <summary>
        /// Page number (1-based).
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Optional language filter (overrides user preferences).
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Optional country filter (overrides user preferences).
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Optional story mode filter.
        /// </summary>
        public StoryMode? Mode { get; set; }
    }

    /// <summary>
    /// Available languages for content preferences.
    /// </summary>
    public class AvailableLanguagesDto
    {
        /// <summary>
        /// List of supported language codes with display names.
        /// </summary>
        public List<LanguageOptionDto> Languages { get; set; } = new();
    }

    /// <summary>
    /// A language option for user selection.
    /// </summary>
    public class LanguageOptionDto
    {
        /// <summary>
        /// ISO 639-1 language code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name in English.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Native name of the language.
        /// </summary>
        public string NativeName { get; set; } = string.Empty;
    }
}
