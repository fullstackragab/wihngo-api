namespace Wihngo.Dtos;

// =============================================
// USER BALANCE
// =============================================

/// <summary>
/// User's USDC balance response
/// </summary>
public class UserBalanceResponse
{
    /// <summary>
    /// Current USDC balance from ledger
    /// </summary>
    public decimal BalanceUsdc { get; set; }

    /// <summary>
    /// Formatted balance string (e.g., "$10.00")
    /// </summary>
    public string FormattedBalance { get; set; } = "$0.00";

    /// <summary>
    /// Whether user has enough SOL for gas (or will be sponsored)
    /// </summary>
    public bool HasGas { get; set; }

    /// <summary>
    /// Whether gas will be sponsored for this user
    /// </summary>
    public bool GasWillBeSponsored { get; set; }

    /// <summary>
    /// User's linked wallet (if any)
    /// </summary>
    public string? WalletPublicKey { get; set; }

    /// <summary>
    /// On-chain USDC balance (if wallet is linked)
    /// </summary>
    public decimal? OnChainBalanceUsdc { get; set; }
}

// =============================================
// LEDGER ENTRY
// =============================================

/// <summary>
/// Single ledger entry response
/// </summary>
public class LedgerEntryResponse
{
    public Guid Id { get; set; }

    /// <summary>
    /// Amount (positive = received, negative = sent)
    /// </summary>
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Entry type: Payment, PaymentReceived, Fee, etc.
    /// </summary>
    public string EntryType { get; set; } = string.Empty;

    /// <summary>
    /// Balance after this entry
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Description of the entry
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference to the source transaction
    /// </summary>
    public Guid ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Paginated ledger entries response
/// </summary>
public class LedgerHistoryResponse
{
    public List<LedgerEntryResponse> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
