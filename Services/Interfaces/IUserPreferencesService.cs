namespace Wihngo.Services.Interfaces
{
    using Wihngo.Dtos;

    /// <summary>
    /// Service for managing user content preferences for smart feed personalization.
    /// </summary>
    public interface IUserPreferencesService
    {
        /// <summary>
        /// Get user's feed preferences including preferred languages and country.
        /// </summary>
        Task<UserPreferencesDto> GetUserPreferencesAsync(Guid userId);

        /// <summary>
        /// Update user's preferred content languages.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="languages">List of ISO 639-1 language codes</param>
        Task UpdatePreferredLanguagesAsync(Guid userId, List<string> languages);

        /// <summary>
        /// Update user's country preference.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="countryCode">ISO 3166-1 alpha-2 country code</param>
        Task UpdateCountryAsync(Guid userId, string countryCode);

        /// <summary>
        /// Set initial preferences for a new user based on their UI language.
        /// Called during registration or first app launch.
        /// </summary>
        Task SetInitialPreferencesAsync(Guid userId, string uiLanguage, string? country = null);
    }
}
