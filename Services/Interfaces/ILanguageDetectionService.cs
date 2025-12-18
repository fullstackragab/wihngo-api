namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for detecting the language of text content.
    /// Used to automatically tag stories with their language for feed personalization.
    /// </summary>
    public interface ILanguageDetectionService
    {
        /// <summary>
        /// Detect the primary language of the given text.
        /// </summary>
        /// <param name="text">Text to analyze (minimum 20 characters recommended)</param>
        /// <returns>ISO 639-1 language code (e.g., "en", "ar", "es") or null if detection fails</returns>
        string? DetectLanguage(string text);

        /// <summary>
        /// Detect the language with a confidence score.
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Tuple of (language code, confidence 0-1) or (null, 0) if detection fails</returns>
        (string? languageCode, double confidence) DetectLanguageWithConfidence(string text);

        /// <summary>
        /// Check if the detected language is one of our supported languages.
        /// </summary>
        /// <param name="languageCode">ISO 639-1 language code</param>
        /// <returns>True if the language is supported by the app</returns>
        bool IsSupportedLanguage(string languageCode);

        /// <summary>
        /// Get the list of supported language codes.
        /// </summary>
        IReadOnlyList<string> SupportedLanguages { get; }
    }
}
