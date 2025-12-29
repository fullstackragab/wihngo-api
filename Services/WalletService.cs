using System.Text;
using Dapper;
using NSec.Cryptography;
using Solnet.Wallet.Utilities;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing user wallet linking (Phantom)
/// </summary>
public class WalletService : IWalletService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IDbConnectionFactory dbFactory,
        ILogger<WalletService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LinkWalletResponse> LinkWalletAsync(Guid userId, LinkWalletRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Check if wallet is already linked to another user
        var existingWallet = await conn.QueryFirstOrDefaultAsync<Wallet>(
            "SELECT * FROM wallets WHERE public_key = @PublicKey",
            new { request.PublicKey });

        if (existingWallet != null)
        {
            if (existingWallet.UserId == userId)
            {
                // Already linked to this user, return existing
                return new LinkWalletResponse
                {
                    WalletId = existingWallet.Id,
                    PublicKey = existingWallet.PublicKey,
                    WalletProvider = existingWallet.WalletProvider,
                    IsPrimary = existingWallet.IsPrimary,
                    CreatedAt = existingWallet.CreatedAt
                };
            }
            else
            {
                throw new InvalidOperationException("This wallet is already linked to another account");
            }
        }

        // Check if user already has a wallet (set this as non-primary if so)
        var userHasWallet = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM wallets WHERE user_id = @UserId)",
            new { UserId = userId });

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PublicKey = request.PublicKey,
            WalletProvider = request.WalletProvider,
            IsPrimary = !userHasWallet, // First wallet is primary
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO wallets (id, user_id, public_key, wallet_provider, is_primary, created_at, updated_at)
              VALUES (@Id, @UserId, @PublicKey, @WalletProvider, @IsPrimary, @CreatedAt, @UpdatedAt)",
            wallet);

        _logger.LogInformation(
            "Wallet linked: User {UserId} linked wallet {PublicKey} (Primary: {IsPrimary})",
            userId, request.PublicKey, wallet.IsPrimary);

        return new LinkWalletResponse
        {
            WalletId = wallet.Id,
            PublicKey = wallet.PublicKey,
            WalletProvider = wallet.WalletProvider,
            IsPrimary = wallet.IsPrimary,
            CreatedAt = wallet.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> UnlinkWalletAsync(Guid userId, Guid walletId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var rowsAffected = await conn.ExecuteAsync(
            "DELETE FROM wallets WHERE id = @WalletId AND user_id = @UserId",
            new { WalletId = walletId, UserId = userId });

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Wallet {WalletId} unlinked from user {UserId}", walletId, userId);

            // If this was the primary wallet, set another one as primary
            await conn.ExecuteAsync(
                @"UPDATE wallets
                  SET is_primary = TRUE, updated_at = @UpdatedAt
                  WHERE user_id = @UserId
                  AND is_primary = FALSE
                  AND id = (SELECT id FROM wallets WHERE user_id = @UserId LIMIT 1)",
                new { UserId = userId, UpdatedAt = DateTime.UtcNow });

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<UserWalletsResponse> GetUserWalletsAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var wallets = await conn.QueryAsync<Wallet>(
            "SELECT * FROM wallets WHERE user_id = @UserId ORDER BY is_primary DESC, created_at ASC",
            new { UserId = userId });

        return new UserWalletsResponse
        {
            Wallets = wallets.Select(w => new WalletResponse
            {
                Id = w.Id,
                PublicKey = w.PublicKey,
                WalletProvider = w.WalletProvider,
                IsPrimary = w.IsPrimary,
                CreatedAt = w.CreatedAt
            }).ToList(),
            Count = wallets.Count()
        };
    }

    /// <inheritdoc />
    public async Task<Wallet?> GetPrimaryWalletAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var wallet = await conn.QueryFirstOrDefaultAsync<Wallet>(
            "SELECT * FROM wallets WHERE user_id = @UserId AND is_primary = TRUE",
            new { UserId = userId });

        // Debug logging
        Console.WriteLine($"[WalletService] GetPrimaryWalletAsync for userId: {userId}");
        Console.WriteLine($"[WalletService] Wallet found: {wallet != null}");
        if (wallet != null)
        {
            Console.WriteLine($"[WalletService] Wallet PublicKey: {wallet.PublicKey}");
        }

        return wallet;
    }

    /// <inheritdoc />
    public async Task<Wallet?> GetWalletByPubkeyAsync(string publicKey)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<Wallet>(
            "SELECT * FROM wallets WHERE public_key = @PublicKey",
            new { PublicKey = publicKey });
    }

    /// <inheritdoc />
    public async Task<Guid?> GetUserIdByWalletAsync(string publicKey)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT user_id FROM wallets WHERE public_key = @PublicKey",
            new { PublicKey = publicKey });
    }

    /// <inheritdoc />
    public async Task<bool> SetPrimaryWalletAsync(Guid userId, Guid walletId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // First, unset all wallets as primary
            await conn.ExecuteAsync(
                "UPDATE wallets SET is_primary = FALSE, updated_at = @UpdatedAt WHERE user_id = @UserId",
                new { UserId = userId, UpdatedAt = DateTime.UtcNow },
                transaction);

            // Set the specified wallet as primary
            var rowsAffected = await conn.ExecuteAsync(
                "UPDATE wallets SET is_primary = TRUE, updated_at = @UpdatedAt WHERE id = @WalletId AND user_id = @UserId",
                new { WalletId = walletId, UserId = userId, UpdatedAt = DateTime.UtcNow },
                transaction);

            transaction.Commit();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Wallet {WalletId} set as primary for user {UserId}", walletId, userId);
                return true;
            }

            return false;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> VerifyWalletOwnershipAsync(string publicKey, string signature, string message)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Wallet ownership verification failed: missing required parameters");
                return Task.FromResult(false);
            }

            // Validate public key length (Solana public keys are 32-44 chars in Base58)
            if (publicKey.Length < 32 || publicKey.Length > 44)
            {
                _logger.LogWarning("Wallet ownership verification failed: invalid public key length {Length}", publicKey.Length);
                return Task.FromResult(false);
            }

            // Validate signature length (Ed25519 signatures are 64 bytes, ~87-88 chars in Base58)
            if (signature.Length < 64 || signature.Length > 100)
            {
                _logger.LogWarning("Wallet ownership verification failed: invalid signature length {Length}", signature.Length);
                return Task.FromResult(false);
            }

            // Decode Base58 public key (32 bytes for Ed25519)
            byte[] publicKeyBytes;
            try
            {
                publicKeyBytes = Encoders.Base58.DecodeData(publicKey);
                if (publicKeyBytes.Length != 32)
                {
                    _logger.LogWarning("Wallet ownership verification failed: decoded public key is {Length} bytes, expected 32", publicKeyBytes.Length);
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wallet ownership verification failed: invalid Base58 public key");
                return Task.FromResult(false);
            }

            // Decode Base58 signature (64 bytes for Ed25519)
            byte[] signatureBytes;
            try
            {
                signatureBytes = Encoders.Base58.DecodeData(signature);
                if (signatureBytes.Length != 64)
                {
                    _logger.LogWarning("Wallet ownership verification failed: decoded signature is {Length} bytes, expected 64", signatureBytes.Length);
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wallet ownership verification failed: invalid Base58 signature");
                return Task.FromResult(false);
            }

            // Encode message as UTF-8 bytes
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Verify Ed25519 signature using NSec
            var algorithm = SignatureAlgorithm.Ed25519;

            // Import the public key
            if (!NSec.Cryptography.PublicKey.TryImport(algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey, out var nsecPublicKey))
            {
                _logger.LogWarning("Wallet ownership verification failed: could not import public key");
                return Task.FromResult(false);
            }

            // Verify the signature
            var isValid = algorithm.Verify(nsecPublicKey, messageBytes, signatureBytes);

            if (isValid)
            {
                _logger.LogInformation("Wallet ownership verification successful for public key: {PublicKey}", publicKey);
            }
            else
            {
                _logger.LogWarning("Wallet ownership verification failed: signature does not match for public key: {PublicKey}", publicKey);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallet ownership verification failed with unexpected error for public key: {PublicKey}", publicKey);
            return Task.FromResult(false);
        }
    }
}
