using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Interface for monitoring Stellar blockchain for asset payments
/// </summary>
public interface IStellarBlockchainMonitor
{
    /// <summary>
    /// Monitor a Stellar account for payments of a specific asset
    /// </summary>
    Task<List<OnChainDeposit>> MonitorAccountAsync(string accountId, string assetCode, string assetIssuer);

    /// <summary>
    /// Get the latest ledger number
    /// </summary>
    Task<long> GetLatestLedgerAsync();

    /// <summary>
    /// Verify a Stellar transaction
    /// </summary>
    Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string assetCode, string assetIssuer);

    /// <summary>
    /// Get payments for an account within a ledger range
    /// </summary>
    Task<List<OnChainDeposit>> GetPaymentsForAccountAsync(
        string accountId, 
        string assetCode, 
        string assetIssuer,
        string? cursor = null,
        int limit = 200);

    /// <summary>
    /// Stream payments for an account in real-time
    /// </summary>
    Task StreamPaymentsAsync(
        string accountId, 
        string assetCode, 
        string assetIssuer,
        Func<OnChainDeposit, Task> onPaymentReceived,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify transaction is finalized
    /// </summary>
    Task<bool> IsTransactionFinalizedAsync(string txHash);
}
