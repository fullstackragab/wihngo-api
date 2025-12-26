using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing user wallet linking
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Links a new wallet to a user's account
    /// </summary>
    Task<LinkWalletResponse> LinkWalletAsync(Guid userId, LinkWalletRequest request);

    /// <summary>
    /// Unlinks a wallet from a user's account
    /// </summary>
    Task<bool> UnlinkWalletAsync(Guid userId, Guid walletId);

    /// <summary>
    /// Gets all wallets linked to a user
    /// </summary>
    Task<UserWalletsResponse> GetUserWalletsAsync(Guid userId);

    /// <summary>
    /// Gets the user's primary wallet
    /// </summary>
    Task<Wallet?> GetPrimaryWalletAsync(Guid userId);

    /// <summary>
    /// Gets a wallet by public key
    /// </summary>
    Task<Wallet?> GetWalletByPubkeyAsync(string publicKey);

    /// <summary>
    /// Gets the user ID associated with a wallet public key
    /// </summary>
    Task<Guid?> GetUserIdByWalletAsync(string publicKey);

    /// <summary>
    /// Sets a wallet as the primary wallet for a user
    /// </summary>
    Task<bool> SetPrimaryWalletAsync(Guid userId, Guid walletId);

    /// <summary>
    /// Verifies wallet ownership via signature
    /// </summary>
    Task<bool> VerifyWalletOwnershipAsync(string publicKey, string signature, string message);
}
