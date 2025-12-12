using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Interface for monitoring EVM-based blockchains (Ethereum, Polygon, Base)
/// Detects ERC-20 token transfers for USDC and EURC
/// </summary>
public interface IEvmBlockchainMonitor
{
    /// <summary>
    /// Monitor a specific address for token transfers
    /// </summary>
    Task<List<OnChainDeposit>> MonitorAddressAsync(string address, string chain, string tokenAddress);

    /// <summary>
    /// Get the latest block number for a chain
    /// </summary>
    Task<long> GetLatestBlockNumberAsync(string chain);

    /// <summary>
    /// Verify a transaction and get its details
    /// </summary>
    Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string chain, string tokenAddress);

    /// <summary>
    /// Scan blocks for Transfer events to monitored addresses
    /// </summary>
    Task<List<OnChainDeposit>> ScanBlocksForDepositsAsync(string chain, long fromBlock, long toBlock, List<string> monitoredAddresses);

    /// <summary>
    /// Get transaction confirmations
    /// </summary>
    Task<int> GetTransactionConfirmationsAsync(string txHash, string chain);
}
