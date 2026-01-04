using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing user USDC ledger balances
/// </summary>
public class LedgerService : ILedgerService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWalletService _walletService;
    private readonly ISolanaTransactionService _solanaService;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(
        IDbConnectionFactory dbFactory,
        IWalletService walletService,
        ISolanaTransactionService solanaService,
        ILogger<LedgerService> logger)
    {
        _dbFactory = dbFactory;
        _walletService = walletService;
        _solanaService = solanaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Get the most recent ledger entry's balance_after, or 0 if no entries
        var balance = await conn.QueryFirstOrDefaultAsync<decimal?>(
            @"SELECT balance_after
              FROM ledger_entries
              WHERE user_id = @UserId
              ORDER BY created_at DESC
              LIMIT 1",
            new { UserId = userId });

        return balance ?? 0m;
    }

    /// <inheritdoc />
    public async Task<UserBalanceResponse> GetFullBalanceAsync(Guid userId)
    {
        var balance = await GetBalanceAsync(userId);
        var wallet = await _walletService.GetPrimaryWalletAsync(userId);

        var response = new UserBalanceResponse
        {
            BalanceUsdc = balance,
            FormattedBalance = $"${balance:F2}",
            WalletPublicKey = wallet?.PublicKey
        };

        if (wallet != null)
        {
            // Get on-chain balance
            response.OnChainBalanceUsdc = await _solanaService.GetUsdcBalanceAsync(wallet.PublicKey);

            // Check if gas will be sponsored
            var solBalance = await _solanaService.GetSolBalanceAsync(wallet.PublicKey);
            response.HasGas = solBalance >= 0.00001m;
            response.GasWillBeSponsored = !response.HasGas;
        }
        else
        {
            response.HasGas = false;
            response.GasWillBeSponsored = true;
        }

        return response;
    }

    /// <inheritdoc />
    public async Task RecordPaymentAsync(P2PPayment payment)
    {
        if (payment.Status != P2PPaymentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot record ledger entries for non-completed payment");
        }

        using var conn = await _dbFactory.CreateOpenConnectionAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // 1. Debit sender (negative amount = payment + fee)
            var senderCurrentBalance = await GetBalanceAsync(payment.SenderUserId);
            var senderNewBalance = senderCurrentBalance - payment.AmountUsdc - payment.FeeUsdc;

            await conn.ExecuteAsync(
                @"INSERT INTO ledger_entries
                  (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
                  VALUES (@Id, @UserId, @Amount, @EntryType, @RefType, @RefId, @BalanceAfter, @Description, @CreatedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = payment.SenderUserId,
                    Amount = -(payment.AmountUsdc + payment.FeeUsdc),
                    EntryType = LedgerEntryType.Payment,
                    RefType = LedgerReferenceType.P2PPayment,
                    RefId = payment.Id,
                    BalanceAfter = senderNewBalance,
                    Description = $"Payment to user - {payment.Memo ?? "No memo"}",
                    CreatedAt = DateTime.UtcNow
                },
                transaction);

            // 2. Credit recipient (positive amount)
            var recipientCurrentBalance = await GetBalanceAsync(payment.RecipientUserId);
            var recipientNewBalance = recipientCurrentBalance + payment.AmountUsdc;

            await conn.ExecuteAsync(
                @"INSERT INTO ledger_entries
                  (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
                  VALUES (@Id, @UserId, @Amount, @EntryType, @RefType, @RefId, @BalanceAfter, @Description, @CreatedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = payment.RecipientUserId,
                    Amount = payment.AmountUsdc,
                    EntryType = LedgerEntryType.PaymentReceived,
                    RefType = LedgerReferenceType.P2PPayment,
                    RefId = payment.Id,
                    BalanceAfter = recipientNewBalance,
                    Description = $"Payment received - {payment.Memo ?? "No memo"}",
                    CreatedAt = DateTime.UtcNow
                },
                transaction);

            // 3. Record fee if applicable (for tracking purposes)
            if (payment.FeeUsdc > 0)
            {
                _logger.LogInformation(
                    "Fee recorded for payment {PaymentId}: {Fee} USDC",
                    payment.Id, payment.FeeUsdc);
            }

            transaction.Commit();

            _logger.LogInformation(
                "Recorded ledger entries for payment {PaymentId}: Sender {Sender} debited {Total} USDC, Recipient {Recipient} credited {Amount} USDC",
                payment.Id, payment.SenderUserId, payment.AmountUsdc + payment.FeeUsdc,
                payment.RecipientUserId, payment.AmountUsdc);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to record ledger entries for payment {PaymentId}", payment.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LedgerHistoryResponse> GetEntriesAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var offset = (page - 1) * pageSize;

        var entries = await conn.QueryAsync<LedgerEntry>(
            @"SELECT * FROM ledger_entries
              WHERE user_id = @UserId
              ORDER BY created_at DESC
              LIMIT @Limit OFFSET @Offset",
            new { UserId = userId, Limit = pageSize, Offset = offset });

        var totalCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ledger_entries WHERE user_id = @UserId",
            new { UserId = userId });

        return new LedgerHistoryResponse
        {
            Entries = entries.Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                AmountUsdc = e.AmountUsdc,
                EntryType = e.EntryType,
                BalanceAfter = e.BalanceAfter,
                Description = e.Description,
                ReferenceId = e.ReferenceId,
                CreatedAt = e.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = offset + pageSize < totalCount
        };
    }

    /// <inheritdoc />
    public async Task<LedgerEntry> CreateAdjustmentAsync(Guid userId, decimal amount, string description)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var currentBalance = await GetBalanceAsync(userId);
        var newBalance = currentBalance + amount;

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AmountUsdc = amount,
            EntryType = LedgerEntryType.Adjustment,
            ReferenceType = LedgerReferenceType.Manual,
            ReferenceId = Guid.NewGuid(),
            BalanceAfter = newBalance,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO ledger_entries
              (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
              VALUES (@Id, @UserId, @AmountUsdc, @EntryType, @ReferenceType, @ReferenceId, @BalanceAfter, @Description, @CreatedAt)",
            entry);

        _logger.LogInformation(
            "Created adjustment entry for user {UserId}: {Amount} USDC - {Description}",
            userId, amount, description);

        return entry;
    }

    /// <inheritdoc />
    public async Task<bool> HasSufficientBalanceAsync(Guid userId, decimal amount)
    {
        var balance = await GetBalanceAsync(userId);
        return balance >= amount;
    }
}
