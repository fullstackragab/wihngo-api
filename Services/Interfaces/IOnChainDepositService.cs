using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing on-chain deposit detection and processing
/// </summary>
public interface IOnChainDepositService
{
    /// <summary>
    /// Record a new deposit detected from blockchain
    /// </summary>
    Task<OnChainDeposit> RecordDepositAsync(OnChainDeposit deposit);

    /// <summary>
    /// Update deposit status and confirmations
    /// </summary>
    Task<OnChainDeposit> UpdateDepositStatusAsync(Guid depositId, string status, int confirmations);

    /// <summary>
    /// Credit a confirmed deposit to user's balance
    /// </summary>
    Task<bool> CreditDepositToUserAsync(Guid depositId);

    /// <summary>
    /// Get deposits by user
    /// </summary>
    Task<List<OnChainDeposit>> GetUserDepositsAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get pending deposits that need confirmation checking
    /// </summary>
    Task<List<OnChainDeposit>> GetPendingDepositsAsync();

    /// <summary>
    /// Check if a transaction hash has already been processed
    /// </summary>
    Task<bool> IsTransactionProcessedAsync(string txHashOrSig);

    /// <summary>
    /// Get token configuration for a specific token and chain
    /// </summary>
    Task<TokenConfiguration?> GetTokenConfigurationAsync(string token, string chain);

    /// <summary>
    /// Get all active token configurations
    /// </summary>
    Task<List<TokenConfiguration>> GetActiveTokenConfigurationsAsync();

    /// <summary>
    /// Get user's derived address for a specific chain
    /// </summary>
    Task<string?> GetUserDerivedAddressAsync(Guid userId, string chain);

    /// <summary>
    /// Register a user's derived address for a specific chain
    /// </summary>
    Task<bool> RegisterUserAddressAsync(Guid userId, string chain, string address, string derivationPath);
}
