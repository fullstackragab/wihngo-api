using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Interface for monitoring Solana blockchain for SPL token transfers
/// </summary>
public interface ISolanaBlockchainMonitor
{
    /// <summary>
    /// Monitor a Solana address for SPL token transfers
    /// </summary>
    Task<List<OnChainDeposit>> MonitorAddressAsync(string address, string mintAddress);

    /// <summary>
    /// Get the latest slot number
    /// </summary>
    Task<long> GetLatestSlotAsync();

    /// <summary>
    /// Verify a Solana transaction signature
    /// </summary>
    Task<TransactionInfo?> VerifyTransactionAsync(string signature, string mintAddress);

    /// <summary>
    /// Get signatures for an address within a slot range
    /// </summary>
    Task<List<string>> GetSignaturesForAddressAsync(string address, long? beforeSlot = null, long? untilSlot = null);

    /// <summary>
    /// Parse transaction for SPL token transfers
    /// </summary>
    Task<OnChainDeposit?> ParseTransactionAsync(string signature, string mintAddress, List<string> monitoredAddresses);

    /// <summary>
    /// Get transaction commitment level (finalized, confirmed)
    /// </summary>
    Task<string> GetTransactionCommitmentAsync(string signature);
}
