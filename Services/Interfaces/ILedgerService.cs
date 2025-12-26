using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing user USDC ledger balances
/// </summary>
public interface ILedgerService
{
    /// <summary>
    /// Gets the current USDC balance for a user
    /// </summary>
    Task<decimal> GetBalanceAsync(Guid userId);

    /// <summary>
    /// Gets the full balance info including on-chain data
    /// </summary>
    Task<UserBalanceResponse> GetFullBalanceAsync(Guid userId);

    /// <summary>
    /// Records ledger entries for a completed P2P payment
    /// Creates debit for sender, credit for recipient, and fee entry if applicable
    /// </summary>
    Task RecordPaymentAsync(P2PPayment payment);

    /// <summary>
    /// Gets paginated ledger entries for a user
    /// </summary>
    Task<LedgerHistoryResponse> GetEntriesAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Creates a manual adjustment entry (admin use)
    /// </summary>
    Task<LedgerEntry> CreateAdjustmentAsync(Guid userId, decimal amount, string description);

    /// <summary>
    /// Checks if user has sufficient balance for a payment
    /// </summary>
    Task<bool> HasSufficientBalanceAsync(Guid userId, decimal amount);
}
