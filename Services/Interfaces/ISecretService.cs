namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for securely retrieving application secrets
/// Supports environment variables and configuration-based secrets
/// </summary>
public interface ISecretService
{
    /// <summary>
    /// Gets a secret value by key
    /// </summary>
    /// <param name="key">The secret key (e.g., "SponsorWalletPrivateKey")</param>
    /// <returns>The secret value, or null if not found</returns>
    Task<string?> GetSecretAsync(string key);

    /// <summary>
    /// Gets a required secret value by key
    /// </summary>
    /// <param name="key">The secret key</param>
    /// <returns>The secret value</returns>
    /// <exception cref="InvalidOperationException">Thrown if secret is not configured</exception>
    Task<string> GetRequiredSecretAsync(string key);
}
