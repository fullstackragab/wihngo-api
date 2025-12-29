using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using Solnet.Wallet.Utilities;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing wallet connection intents.
///
/// Solves the Android browser-switch problem:
/// When Phantom redirects back after signing, Android may open a different browser,
/// losing the user's JWT session. This service provides stateless recovery.
/// </summary>
public class WalletConnectIntentService : IWalletConnectIntentService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWalletService _walletService;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WalletConnectIntentService> _logger;

    // Intent expiration time in minutes
    private const int IntentExpirationMinutes = 10;

    public WalletConnectIntentService(
        IDbConnectionFactory dbFactory,
        IWalletService walletService,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<WalletConnectIntentService> logger)
    {
        _dbFactory = dbFactory;
        _walletService = walletService;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WalletConnectIntentResponse> CreateIntentAsync(
        Guid? userId,
        CreateWalletConnectIntentRequest request,
        string? ipAddress = null,
        string? userAgent = null)
    {
        // Generate cryptographically secure state and nonce
        var state = GenerateSecureToken(32);
        var nonce = GenerateSecureToken(16);

        // Create the message to sign
        var message = BuildSignMessage(nonce);

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var intent = new WalletConnectIntent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            State = state,
            Nonce = nonce,
            Purpose = request.Purpose,
            Status = WalletConnectIntentStatus.Pending,
            WalletProvider = request.WalletProvider,
            RedirectUrl = request.RedirectUrl,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddMinutes(IntentExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO wallet_connect_intents
              (id, user_id, state, nonce, purpose, status, wallet_provider, redirect_url, metadata, ip_address, user_agent, expires_at, created_at, updated_at)
              VALUES (@Id, @UserId, @State, @Nonce, @Purpose, @Status, @WalletProvider, @RedirectUrl, @Metadata::jsonb, @IpAddress, @UserAgent, @ExpiresAt, @CreatedAt, @UpdatedAt)",
            intent);

        _logger.LogInformation(
            "Wallet connect intent created: {IntentId} for user {UserId}, purpose: {Purpose}",
            intent.Id, userId, request.Purpose);

        // Build callback URL
        var baseUrl = _configuration["Base:ApiUrl"] ?? "https://api.wihngo.com";
        var callbackUrl = $"{baseUrl}/api/wallet-connect/callback";

        return new WalletConnectIntentResponse
        {
            IntentId = intent.Id,
            State = state,
            Nonce = nonce,
            Message = message,
            CallbackUrl = callbackUrl,
            ExpiresAt = intent.ExpiresAt,
            CreatedAt = intent.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<WalletConnectCallbackResponse> ProcessCallbackAsync(WalletConnectCallbackRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Find intent by state
        var intent = await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
            "SELECT * FROM wallet_connect_intents WHERE state = @State",
            new { request.State });

        if (intent == null)
        {
            _logger.LogWarning("Wallet connect callback with invalid state: {State}", request.State);
            return new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.IntentNotFound,
                Message = "Invalid or expired connection request"
            };
        }

        // 2. Check if already used
        if (intent.Status == WalletConnectIntentStatus.Completed)
        {
            return new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.IntentAlreadyUsed,
                Message = "This connection request has already been used"
            };
        }

        // 3. Check if expired
        if (intent.IsExpired)
        {
            await UpdateIntentStatus(conn, intent.Id, WalletConnectIntentStatus.Expired);
            return new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.IntentExpired,
                Message = "Connection request has expired. Please try again."
            };
        }

        // 4. Check if cancelled
        if (intent.Status == WalletConnectIntentStatus.Cancelled)
        {
            return new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.IntentCancelled,
                Message = "Connection request was cancelled"
            };
        }

        // 5. Update status to processing
        await UpdateIntentStatus(conn, intent.Id, WalletConnectIntentStatus.Processing);

        // 6. Verify signature
        var message = BuildSignMessage(intent.Nonce);
        var signatureValid = await VerifySignatureAsync(request.PublicKey, message, request.Signature);

        if (!signatureValid)
        {
            await UpdateIntentStatus(conn, intent.Id, WalletConnectIntentStatus.Failed);
            _logger.LogWarning(
                "Invalid signature for wallet connect intent {IntentId}, pubkey: {PublicKey}",
                intent.Id, request.PublicKey);

            return new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.SignatureVerificationFailed,
                Message = "Wallet signature verification failed"
            };
        }

        // 7. Link wallet (if user was authenticated)
        Guid? walletId = null;
        bool isNewWallet = false;

        if (intent.UserId.HasValue)
        {
            try
            {
                // Check if wallet is already linked
                var existingWallet = await _walletService.GetWalletByPubkeyAsync(request.PublicKey);

                if (existingWallet != null)
                {
                    if (existingWallet.UserId == intent.UserId.Value)
                    {
                        // Already linked to this user - just return success
                        walletId = existingWallet.Id;
                        isNewWallet = false;
                    }
                    else
                    {
                        // Linked to another user
                        await UpdateIntentStatus(conn, intent.Id, WalletConnectIntentStatus.Failed);
                        return new WalletConnectCallbackResponse
                        {
                            Success = false,
                            ErrorCode = WalletConnectErrorCodes.WalletAlreadyLinked,
                            Message = "This wallet is already linked to another account"
                        };
                    }
                }
                else
                {
                    // Link new wallet
                    var linkResponse = await _walletService.LinkWalletAsync(intent.UserId.Value, new LinkWalletRequest
                    {
                        PublicKey = request.PublicKey,
                        WalletProvider = intent.WalletProvider
                    });
                    walletId = linkResponse.WalletId;
                    isNewWallet = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link wallet for intent {IntentId}", intent.Id);
                await UpdateIntentStatus(conn, intent.Id, WalletConnectIntentStatus.Failed);
                return new WalletConnectCallbackResponse
                {
                    Success = false,
                    ErrorCode = WalletConnectErrorCodes.WalletLinkFailed,
                    Message = "Failed to link wallet"
                };
            }
        }

        // 8. Update intent with wallet details
        await conn.ExecuteAsync(
            @"UPDATE wallet_connect_intents
              SET status = @Status, public_key = @PublicKey, signature = @Signature,
                  completed_at = @CompletedAt, updated_at = @UpdatedAt
              WHERE id = @IntentId",
            new
            {
                Status = WalletConnectIntentStatus.Completed,
                request.PublicKey,
                request.Signature,
                CompletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IntentId = intent.Id
            });

        _logger.LogInformation(
            "Wallet connect completed: Intent {IntentId}, User {UserId}, Wallet {PublicKey}, New: {IsNew}",
            intent.Id, intent.UserId, request.PublicKey, isNewWallet);

        // 9. Generate new tokens if user was authenticated (for session recovery)
        string? accessToken = null;
        string? refreshToken = null;

        if (intent.UserId.HasValue)
        {
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE user_id = @UserId",
                new { UserId = intent.UserId.Value });

            if (user != null)
            {
                var (token, _) = _tokenService.GenerateToken(user);
                accessToken = token;
                refreshToken = _tokenService.GenerateRefreshToken();

                // Store refresh token hash
                var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
                await conn.ExecuteAsync(
                    @"UPDATE users SET refresh_token_hash = @Hash, refresh_token_expiry = @Expiry, updated_at = @UpdatedAt
                      WHERE user_id = @UserId",
                    new
                    {
                        Hash = refreshTokenHash,
                        Expiry = DateTime.UtcNow.AddDays(30),
                        UpdatedAt = DateTime.UtcNow,
                        UserId = intent.UserId.Value
                    });
            }
        }

        // 10. Parse metadata for response
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrEmpty(intent.Metadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(intent.Metadata);
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return new WalletConnectCallbackResponse
        {
            Success = true,
            WalletId = walletId,
            PublicKey = request.PublicKey,
            IsNewWallet = isNewWallet,
            UserId = intent.UserId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RedirectUrl = intent.RedirectUrl,
            Metadata = metadata,
            Message = isNewWallet ? "Wallet connected successfully" : "Wallet already connected"
        };
    }

    /// <inheritdoc />
    public async Task<WalletConnectIntentStatusResponse?> GetIntentStatusAsync(
        string intentIdOrState,
        Guid? userId = null)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        WalletConnectIntent? intent;

        // Try as GUID first
        if (Guid.TryParse(intentIdOrState, out var intentId))
        {
            intent = await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
                "SELECT * FROM wallet_connect_intents WHERE id = @IntentId",
                new { IntentId = intentId });
        }
        else
        {
            // Try as state token
            intent = await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
                "SELECT * FROM wallet_connect_intents WHERE state = @State",
                new { State = intentIdOrState });
        }

        if (intent == null) return null;

        // Optionally validate ownership
        if (userId.HasValue && intent.UserId.HasValue && intent.UserId.Value != userId.Value)
        {
            return null; // Don't reveal intent exists if not owner
        }

        return new WalletConnectIntentStatusResponse
        {
            IntentId = intent.Id,
            Status = intent.Status,
            PublicKey = intent.PublicKey,
            IsActive = !intent.IsExpired &&
                       intent.Status != WalletConnectIntentStatus.Completed &&
                       intent.Status != WalletConnectIntentStatus.Cancelled &&
                       intent.Status != WalletConnectIntentStatus.Failed &&
                       intent.Status != WalletConnectIntentStatus.Expired,
            ExpiresAt = intent.ExpiresAt,
            CompletedAt = intent.CompletedAt
        };
    }

    /// <inheritdoc />
    public async Task<PendingWalletIntentResponse> GetPendingIntentAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var intent = await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
            @"SELECT * FROM wallet_connect_intents
              WHERE user_id = @UserId
              AND status IN (@Pending, @AwaitingCallback)
              AND expires_at > @Now
              ORDER BY created_at DESC
              LIMIT 1",
            new
            {
                UserId = userId,
                Pending = WalletConnectIntentStatus.Pending,
                AwaitingCallback = WalletConnectIntentStatus.AwaitingCallback,
                Now = DateTime.UtcNow
            });

        if (intent == null)
        {
            return new PendingWalletIntentResponse
            {
                HasPendingIntent = false,
                Message = "No pending wallet connection"
            };
        }

        return new PendingWalletIntentResponse
        {
            HasPendingIntent = true,
            Intent = new WalletConnectIntentStatusResponse
            {
                IntentId = intent.Id,
                Status = intent.Status,
                IsActive = true,
                ExpiresAt = intent.ExpiresAt
            },
            Message = "You have a pending wallet connection"
        };
    }

    /// <inheritdoc />
    public async Task<bool> CancelIntentAsync(string intentIdOrState, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        int rowsAffected;

        if (Guid.TryParse(intentIdOrState, out var intentId))
        {
            rowsAffected = await conn.ExecuteAsync(
                @"UPDATE wallet_connect_intents
                  SET status = @Status, updated_at = @UpdatedAt
                  WHERE id = @IntentId AND user_id = @UserId
                  AND status IN (@Pending, @AwaitingCallback)",
                new
                {
                    Status = WalletConnectIntentStatus.Cancelled,
                    UpdatedAt = DateTime.UtcNow,
                    IntentId = intentId,
                    UserId = userId,
                    Pending = WalletConnectIntentStatus.Pending,
                    AwaitingCallback = WalletConnectIntentStatus.AwaitingCallback
                });
        }
        else
        {
            rowsAffected = await conn.ExecuteAsync(
                @"UPDATE wallet_connect_intents
                  SET status = @Status, updated_at = @UpdatedAt
                  WHERE state = @State AND user_id = @UserId
                  AND status IN (@Pending, @AwaitingCallback)",
                new
                {
                    Status = WalletConnectIntentStatus.Cancelled,
                    UpdatedAt = DateTime.UtcNow,
                    State = intentIdOrState,
                    UserId = userId,
                    Pending = WalletConnectIntentStatus.Pending,
                    AwaitingCallback = WalletConnectIntentStatus.AwaitingCallback
                });
        }

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<WalletConnectIntent?> GetIntentByStateAsync(string state)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
            "SELECT * FROM wallet_connect_intents WHERE state = @State",
            new { State = state });
    }

    /// <inheritdoc />
    public async Task<WalletConnectIntent?> GetIntentByIdAsync(Guid intentId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<WalletConnectIntent>(
            "SELECT * FROM wallet_connect_intents WHERE id = @IntentId",
            new { IntentId = intentId });
    }

    /// <inheritdoc />
    public async Task<int> ExpireOldIntentsAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var rowsAffected = await conn.ExecuteAsync(
            @"UPDATE wallet_connect_intents
              SET status = @Status, updated_at = @UpdatedAt
              WHERE status IN (@Pending, @AwaitingCallback)
              AND expires_at < @Now",
            new
            {
                Status = WalletConnectIntentStatus.Expired,
                UpdatedAt = DateTime.UtcNow,
                Now = DateTime.UtcNow,
                Pending = WalletConnectIntentStatus.Pending,
                AwaitingCallback = WalletConnectIntentStatus.AwaitingCallback
            });

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Expired {Count} wallet connect intents", rowsAffected);
        }

        return rowsAffected;
    }

    /// <inheritdoc />
    public Task<bool> VerifySignatureAsync(string publicKey, string message, string signature)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Signature verification failed: missing required parameters");
                return Task.FromResult(false);
            }

            // Validate public key length (Solana public keys are 32-44 chars in Base58)
            if (publicKey.Length < 32 || publicKey.Length > 44)
            {
                _logger.LogWarning("Signature verification failed: invalid public key length {Length}", publicKey.Length);
                return Task.FromResult(false);
            }

            // Validate signature length (Ed25519 signatures are 64 bytes, ~87-88 chars in Base58)
            if (signature.Length < 64 || signature.Length > 100)
            {
                _logger.LogWarning("Signature verification failed: invalid signature length {Length}", signature.Length);
                return Task.FromResult(false);
            }

            // Decode Base58 public key (32 bytes for Ed25519)
            byte[] publicKeyBytes;
            try
            {
                publicKeyBytes = Encoders.Base58.DecodeData(publicKey);
                if (publicKeyBytes.Length != 32)
                {
                    _logger.LogWarning("Signature verification failed: decoded public key is {Length} bytes, expected 32", publicKeyBytes.Length);
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Signature verification failed: invalid Base58 public key");
                return Task.FromResult(false);
            }

            // Decode Base58 signature (64 bytes for Ed25519)
            byte[] signatureBytes;
            try
            {
                signatureBytes = Encoders.Base58.DecodeData(signature);
                if (signatureBytes.Length != 64)
                {
                    _logger.LogWarning("Signature verification failed: decoded signature is {Length} bytes, expected 64", signatureBytes.Length);
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Signature verification failed: invalid Base58 signature");
                return Task.FromResult(false);
            }

            // Encode message as UTF-8 bytes
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Verify Ed25519 signature using NSec
            var algorithm = SignatureAlgorithm.Ed25519;

            // Import the public key
            if (!NSec.Cryptography.PublicKey.TryImport(algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey, out var nsecPublicKey))
            {
                _logger.LogWarning("Signature verification failed: could not import public key");
                return Task.FromResult(false);
            }

            // Verify the signature
            var isValid = algorithm.Verify(nsecPublicKey, messageBytes, signatureBytes);

            if (isValid)
            {
                _logger.LogInformation("Signature verification successful for public key: {PublicKey}", publicKey);
            }
            else
            {
                _logger.LogWarning("Signature verification failed: signature does not match for public key: {PublicKey}", publicKey);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification failed with unexpected error for public key: {PublicKey}", publicKey);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Builds the message for the user to sign
    /// </summary>
    private string BuildSignMessage(string nonce)
    {
        var appName = _configuration["Base:AppName"] ?? "Wihngo";
        return $"Sign this message to connect your wallet to {appName}.\n\nNonce: {nonce}";
    }

    /// <summary>
    /// Updates the status of an intent
    /// </summary>
    private static async Task UpdateIntentStatus(System.Data.IDbConnection conn, Guid intentId, string status)
    {
        await conn.ExecuteAsync(
            "UPDATE wallet_connect_intents SET status = @Status, updated_at = @UpdatedAt WHERE id = @IntentId",
            new { Status = status, UpdatedAt = DateTime.UtcNow, IntentId = intentId });
    }
}
