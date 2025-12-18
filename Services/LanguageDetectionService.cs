using NTextCat;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Language detection service using NTextCat library.
    /// Detects the language of text content for feed personalization.
    /// </summary>
    public class LanguageDetectionService : ILanguageDetectionService
    {
        private readonly RankedLanguageIdentifier _identifier;
        private readonly ILogger<LanguageDetectionService> _logger;
        private const int MinimumTextLength = 20;
        private const double MinimumConfidence = 0.5;

        /// <summary>
        /// Supported languages matching the app's i18n configuration.
        /// ISO 639-1 codes.
        /// </summary>
        private static readonly HashSet<string> _supportedLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", // Arabic
            "de", // German
            "en", // English
            "es", // Spanish
            "fr", // French
            "hi", // Hindi
            "id", // Indonesian
            "it", // Italian
            "ja", // Japanese
            "ko", // Korean
            "pl", // Polish
            "pt", // Portuguese
            "th", // Thai
            "tr", // Turkish
            "vi", // Vietnamese
            "zh"  // Chinese
        };

        /// <summary>
        /// Mapping from NTextCat's 3-letter codes to ISO 639-1 codes.
        /// NTextCat uses ISO 639-3 codes which need to be converted.
        /// </summary>
        private static readonly Dictionary<string, string> _languageCodeMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            // Arabic variants
            { "ara", "ar" }, { "arb", "ar" },
            // German
            { "deu", "de" }, { "ger", "de" },
            // English
            { "eng", "en" },
            // Spanish
            { "spa", "es" },
            // French
            { "fra", "fr" }, { "fre", "fr" },
            // Hindi
            { "hin", "hi" },
            // Indonesian
            { "ind", "id" },
            // Italian
            { "ita", "it" },
            // Japanese
            { "jpn", "ja" },
            // Korean
            { "kor", "ko" },
            // Polish
            { "pol", "pl" },
            // Portuguese
            { "por", "pt" },
            // Thai
            { "tha", "th" },
            // Turkish
            { "tur", "tr" },
            // Vietnamese
            { "vie", "vi" },
            // Chinese variants
            { "zho", "zh" }, { "chi", "zh" }, { "cmn", "zh" }
        };

        public IReadOnlyList<string> SupportedLanguages => _supportedLanguages.ToList().AsReadOnly();

        public LanguageDetectionService(ILogger<LanguageDetectionService> logger)
        {
            _logger = logger;

            try
            {
                // Initialize NTextCat with the default language profile
                var factory = new RankedLanguageIdentifierFactory();
                _identifier = factory.Load("Core14.profile.xml");
                _logger.LogInformation("Language detection service initialized with {Count} profiles",
                    _identifier.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize language detection service. Using fallback.");
                // Create a minimal identifier that will return unknown
                _identifier = null!;
            }
        }

        public string? DetectLanguage(string text)
        {
            var (languageCode, _) = DetectLanguageWithConfidence(text);
            return languageCode;
        }

        public (string? languageCode, double confidence) DetectLanguageWithConfidence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Empty text provided for language detection");
                return (null, 0);
            }

            // Clean the text - remove URLs, mentions, hashtags
            var cleanedText = CleanText(text);

            if (cleanedText.Length < MinimumTextLength)
            {
                _logger.LogDebug("Text too short for reliable detection: {Length} chars (minimum: {Min})",
                    cleanedText.Length, MinimumTextLength);
                return (null, 0);
            }

            if (_identifier == null)
            {
                _logger.LogWarning("Language identifier not initialized, cannot detect language");
                return (null, 0);
            }

            try
            {
                var languages = _identifier.Identify(cleanedText);
                var topResult = languages.FirstOrDefault();

                if (topResult == null)
                {
                    _logger.LogDebug("No language detected for text");
                    return (null, 0);
                }

                // Get the ISO 639-3 code from NTextCat
                var detectedCode = topResult.Item1.Iso639_3;

                // Convert to ISO 639-1 code
                var iso6391Code = ConvertToIso6391(detectedCode);

                if (iso6391Code == null)
                {
                    _logger.LogDebug("Detected language code {Code} could not be mapped to ISO 639-1", detectedCode);
                    return (null, 0);
                }

                // Calculate confidence (NTextCat doesn't give direct confidence, so we estimate)
                // Lower score is better in NTextCat, so we invert it
                var allLanguages = languages.ToList();
                double confidence = 1.0;

                if (allLanguages.Count > 1)
                {
                    var firstScore = allLanguages[0].Item2;
                    var secondScore = allLanguages[1].Item2;
                    // If the gap between first and second is large, confidence is high
                    confidence = Math.Min(1.0, Math.Abs(secondScore - firstScore) / Math.Max(1, firstScore));
                }

                // Check if it's a supported language
                if (!IsSupportedLanguage(iso6391Code))
                {
                    _logger.LogDebug("Detected language {Code} is not in supported languages list", iso6391Code);
                    // Still return it but with lower confidence indicator
                    return (iso6391Code, confidence * 0.5);
                }

                _logger.LogDebug("Detected language: {Code} with confidence {Confidence:P0}",
                    iso6391Code, confidence);
                return (iso6391Code, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting language for text");
                return (null, 0);
            }
        }

        public bool IsSupportedLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            return _supportedLanguages.Contains(languageCode);
        }

        /// <summary>
        /// Convert ISO 639-3 code to ISO 639-1 code.
        /// </summary>
        private string? ConvertToIso6391(string? iso6393Code)
        {
            if (string.IsNullOrWhiteSpace(iso6393Code))
                return null;

            // Try direct mapping
            if (_languageCodeMapping.TryGetValue(iso6393Code, out var iso6391))
                return iso6391;

            // If the code is already 2 letters, it might be ISO 639-1
            if (iso6393Code.Length == 2 && _supportedLanguages.Contains(iso6393Code))
                return iso6393Code.ToLowerInvariant();

            return null;
        }

        /// <summary>
        /// Clean text for better language detection accuracy.
        /// Removes URLs, mentions, hashtags, and excessive punctuation.
        /// </summary>
        private static string CleanText(string text)
        {
            // Remove URLs
            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                text, @"https?://\S+|www\.\S+", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove mentions (@username)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"@\w+", " ");

            // Remove hashtags (#tag) but keep the word
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"#(\w+)", "$1");

            // Remove emojis and special characters that aren't part of any language
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[\U0001F600-\U0001F64F\U0001F300-\U0001F5FF\U0001F680-\U0001F6FF\U0001F1E0-\U0001F1FF]", " ");

            // Normalize whitespace
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }
    }
}
