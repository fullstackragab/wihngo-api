using Dapper;
using System.Text.Json;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service for managing user content preferences.
    /// </summary>
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<UserPreferencesService> _logger;

        /// <summary>
        /// Supported language codes matching the app's i18n configuration.
        /// </summary>
        private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "de", "en", "es", "fr", "hi", "id", "it",
            "ja", "ko", "pl", "pt", "th", "tr", "vi", "zh"
        };

        public UserPreferencesService(
            IDbConnectionFactory dbFactory,
            ILogger<UserPreferencesService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<UserPreferencesDto> GetUserPreferencesAsync(Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "SELECT preferred_languages_json, country FROM users WHERE user_id = @UserId";
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId });

            if (result == null)
            {
                _logger.LogWarning("User {UserId} not found when getting preferences", userId);
                return new UserPreferencesDto();
            }

            var preferredLanguagesJson = (string?)result.preferred_languages_json;
            var country = (string?)result.country;

            return new UserPreferencesDto
            {
                PreferredLanguages = ParseLanguages(preferredLanguagesJson),
                Country = country
            };
        }

        public async Task UpdatePreferredLanguagesAsync(Guid userId, List<string> languages)
        {
            // Validate and filter languages
            var validLanguages = languages
                .Where(l => SupportedLanguages.Contains(l))
                .Select(l => l.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (validLanguages.Count != languages.Count)
            {
                var invalidLanguages = languages.Except(validLanguages, StringComparer.OrdinalIgnoreCase);
                _logger.LogWarning("User {UserId} tried to set invalid languages: {Invalid}",
                    userId, string.Join(", ", invalidLanguages));
            }

            var languagesJson = JsonSerializer.Serialize(validLanguages);

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "UPDATE users SET preferred_languages_json = @Languages WHERE user_id = @UserId";
            var updated = await connection.ExecuteAsync(sql, new { Languages = languagesJson, UserId = userId });

            if (updated == 0)
            {
                _logger.LogWarning("User {UserId} not found when updating preferred languages", userId);
            }
            else
            {
                _logger.LogInformation("Updated preferred languages for user {UserId}: {Languages}",
                    userId, string.Join(", ", validLanguages));
            }
        }

        public async Task UpdateCountryAsync(Guid userId, string countryCode)
        {
            // Validate country code (basic validation - 2 letters)
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            {
                throw new ArgumentException("Country code must be a 2-letter ISO 3166-1 alpha-2 code", nameof(countryCode));
            }

            var normalizedCountry = countryCode.ToUpperInvariant();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = "UPDATE users SET country = @Country WHERE user_id = @UserId";
            var updated = await connection.ExecuteAsync(sql, new { Country = normalizedCountry, UserId = userId });

            if (updated == 0)
            {
                _logger.LogWarning("User {UserId} not found when updating country", userId);
            }
            else
            {
                _logger.LogInformation("Updated country for user {UserId}: {Country}", userId, normalizedCountry);
            }
        }

        public async Task SetInitialPreferencesAsync(Guid userId, string uiLanguage, string? country = null)
        {
            // Default to the UI language as the preferred content language
            var preferredLanguages = new List<string>();

            if (SupportedLanguages.Contains(uiLanguage))
            {
                preferredLanguages.Add(uiLanguage.ToLowerInvariant());
            }

            var languagesJson = JsonSerializer.Serialize(preferredLanguages);
            var normalizedCountry = !string.IsNullOrWhiteSpace(country) && country.Length == 2
                ? country.ToUpperInvariant()
                : null;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var sql = @"
                UPDATE users
                SET preferred_languages_json = @Languages, country = @Country
                WHERE user_id = @UserId
                  AND (preferred_languages_json IS NULL OR preferred_languages_json = '[]')";

            var updated = await connection.ExecuteAsync(sql, new
            {
                Languages = languagesJson,
                Country = normalizedCountry,
                UserId = userId
            });

            if (updated > 0)
            {
                _logger.LogInformation("Set initial preferences for user {UserId}: languages={Languages}, country={Country}",
                    userId, string.Join(", ", preferredLanguages), normalizedCountry ?? "not set");
            }
        }

        /// <summary>
        /// Parse languages JSON into a list.
        /// </summary>
        private static List<string> ParseLanguages(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
