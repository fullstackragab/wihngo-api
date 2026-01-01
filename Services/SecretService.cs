using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for retrieving secrets from environment variables or configuration
/// For production, consider using Azure Key Vault or AWS Secrets Manager
/// </summary>
public class SecretService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretService> _logger;

    // Mapping of secret keys to environment variable names
    private static readonly Dictionary<string, string> SecretKeyToEnvVar = new()
    {
        { "SponsorWalletPrivateKey", "SPONSOR_WALLET_PRIVATE_KEY" },
        { "SendGridApiKey", "SENDGRID_API_KEY" },
        { "JwtKey", "JWT_KEY" }
    };

    public SecretService(IConfiguration configuration, ILogger<SecretService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(string key)
    {
        // First try environment variable
        if (SecretKeyToEnvVar.TryGetValue(key, out var envVarName))
        {
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(envValue))
            {
                return Task.FromResult<string?>(envValue);
            }
        }

        // Fallback to configuration (for development only - not recommended for production)
        var configPath = $"Secrets:{key}";
        var configValue = _configuration[configPath];
        if (!string.IsNullOrEmpty(configValue))
        {
            _logger.LogWarning(
                "Secret '{Key}' loaded from configuration. Use environment variables in production",
                key);
            return Task.FromResult<string?>(configValue);
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public async Task<string> GetRequiredSecretAsync(string key)
    {
        var secret = await GetSecretAsync(key);
        if (string.IsNullOrEmpty(secret))
        {
            var envVarName = SecretKeyToEnvVar.GetValueOrDefault(key, key);
            throw new InvalidOperationException(
                $"Required secret '{key}' is not configured. " +
                $"Set the environment variable '{envVarName}' or configure 'Secrets:{key}'");
        }
        return secret;
    }
}
