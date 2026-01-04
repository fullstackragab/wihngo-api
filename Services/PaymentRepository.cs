using Dapper;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Repository for payment ledger operations.
/// </summary>
public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _db;

    public PaymentRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    // -------------------------------------------------------------------------
    // READ
    // -------------------------------------------------------------------------

    public async Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                buyer_email AS BuyerEmail,
                sweep_eligible_at AS SweepEligibleAt,
                treasury_tx_hash AS TreasuryTxHash,
                swept_at AS SweptAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE id = @PaymentId
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(
            new CommandDefinition(sql, new { PaymentId = paymentId }, cancellationToken: ct));

        return row?.ToEntity();
    }

    public async Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE provider_ref = @ProviderRef
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(
            new CommandDefinition(sql, new { ProviderRef = providerRef }, cancellationToken: ct));

        return row?.ToEntity();
    }

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetStalePendingPaymentsAsync(
        TimeSpan olderThan,
        int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE status = 'pending'
              AND created_at < @CutoffTime
            ORDER BY created_at ASC
            LIMIT @Limit
            """;

        var cutoffTime = DateTime.UtcNow - olderThan;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { CutoffTime = cutoffTime, Limit = limit }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetOrphanedConfirmedPaymentsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        // Find confirmed bird support payments that don't have a corresponding support_transaction record
        const string sql = """
            SELECT
                pay.id AS Id,
                pay.user_id AS UserId,
                pay.bird_id AS BirdId,
                pay.purpose AS Purpose,
                pay.amount_cents AS AmountCents,
                pay.currency AS Currency,
                pay.provider AS Provider,
                pay.provider_ref AS ProviderRef,
                pay.status AS Status,
                pay.created_at AS CreatedAt,
                pay.confirmed_at AS ConfirmedAt,
                pay.wihngo_amount_cents AS WihngoAmountCents
            FROM payments pay
            LEFT JOIN support_transactions st ON st.payment_id = pay.id
            WHERE pay.status = 'confirmed'
              AND pay.purpose = 'BIRD_SUPPORT'
              AND pay.bird_id IS NOT NULL
              AND st.id IS NULL
            ORDER BY pay.confirmed_at ASC
            LIMIT @Limit
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    // -------------------------------------------------------------------------
    // WRITE
    // -------------------------------------------------------------------------

    public async Task InsertAsync(Payment payment, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO payments (
                id, user_id, bird_id, purpose, amount_cents, currency,
                provider, provider_ref, status, created_at, confirmed_at,
                wihngo_amount_cents
            )
            VALUES (
                @Id, @UserId, @BirdId, @Purpose, @AmountCents, @Currency,
                @Provider, @ProviderRef, @Status, @CreatedAt, @ConfirmedAt,
                @WihngoAmountCents
            )
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                payment.Id,
                payment.UserId,
                payment.BirdId,
                Purpose = PurposeToDb(payment.Purpose),
                payment.AmountCents,
                payment.Currency,
                Provider = ProviderToDb(payment.Provider),
                payment.ProviderRef,
                Status = StatusToDb(payment.Status),
                payment.CreatedAt,
                payment.ConfirmedAt,
                payment.WihngoAmountCents
            }, cancellationToken: ct));
    }

    public async Task ConfirmAsync(Guid paymentId, string providerRef, DateTime confirmedAt, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET status = 'confirmed',
                provider_ref = @ProviderRef,
                confirmed_at = @ConfirmedAt
            WHERE id = @Id AND status = 'pending'
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = paymentId,
                ProviderRef = providerRef,
                ConfirmedAt = confirmedAt
            }, cancellationToken: ct));
    }

    public async Task FailAsync(Guid paymentId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET status = 'failed'
            WHERE id = @Id AND status = 'pending'
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId }, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // MANUAL PAYMENT METHODS
    // -------------------------------------------------------------------------

    public async Task InsertManualPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO payments (
                id, user_id, bird_id, purpose, amount_cents, currency,
                provider, provider_ref, status, created_at, confirmed_at,
                destination_address, derivation_index, expires_at, buyer_email,
                wihngo_amount_cents
            )
            VALUES (
                @Id, @UserId, @BirdId, @Purpose, @AmountCents, @Currency,
                @Provider, @ProviderRef, @Status, @CreatedAt, @ConfirmedAt,
                @DestinationAddress, @DerivationIndex, @ExpiresAt, @BuyerEmail,
                @WihngoAmountCents
            )
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                payment.Id,
                payment.UserId,
                payment.BirdId,
                Purpose = PurposeToDb(payment.Purpose),
                payment.AmountCents,
                payment.Currency,
                Provider = ProviderToDb(payment.Provider),
                payment.ProviderRef,
                Status = StatusToDb(payment.Status),
                payment.CreatedAt,
                payment.ConfirmedAt,
                payment.DestinationAddress,
                payment.DerivationIndex,
                payment.ExpiresAt,
                payment.BuyerEmail,
                payment.WihngoAmountCents
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Payment>> GetPendingManualPaymentsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                buyer_email AS BuyerEmail,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE status = 'pending'
              AND provider = 'MANUAL_USDC_SOLANA'
              AND destination_address IS NOT NULL
            ORDER BY created_at ASC
            LIMIT @Limit
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task ExpireAsync(Guid paymentId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET status = 'expired'
            WHERE id = @Id AND status = 'pending'
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId }, cancellationToken: ct));
    }

    public async Task ClaimAsync(Guid paymentId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET user_id = @UserId,
                claimed_at = @ClaimedAt
            WHERE id = @Id
              AND claimed_at IS NULL
              AND status = 'confirmed'
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId, UserId = userId, ClaimedAt = DateTime.UtcNow }, cancellationToken: ct));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Payment cannot be claimed. Either already claimed, not confirmed, or not found.");
    }

    // -------------------------------------------------------------------------
    // ADMIN OPERATIONS
    // -------------------------------------------------------------------------

    public async Task<IReadOnlyList<Payment>> GetUnclaimedConfirmedPaymentsAsync(
        int limit = 100,
        int offset = 0,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE status = 'confirmed'
              AND user_id IS NULL
              AND claimed_at IS NULL
            ORDER BY confirmed_at DESC
            LIMIT @Limit OFFSET @Offset
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Limit = limit, Offset = offset }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<int> CountUnclaimedConfirmedPaymentsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM payments
            WHERE status = 'confirmed'
              AND user_id IS NULL
              AND claimed_at IS NULL
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task AdminAssignAsync(Guid paymentId, Guid userId, Guid adminId, string? reason, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET user_id = @UserId,
                claimed_at = @ClaimedAt
            WHERE id = @Id
              AND status = 'confirmed'
              AND user_id IS NULL
              AND claimed_at IS NULL
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId, UserId = userId, ClaimedAt = DateTime.UtcNow }, cancellationToken: ct));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Payment cannot be assigned. Either already claimed, not confirmed, or not found.");
    }

    // -------------------------------------------------------------------------
    // SWEEP OPERATIONS
    // -------------------------------------------------------------------------

    public async Task<IReadOnlyList<Payment>> GetSweepEligiblePaymentsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                buyer_email AS BuyerEmail,
                sweep_eligible_at AS SweepEligibleAt,
                treasury_tx_hash AS TreasuryTxHash,
                swept_at AS SweptAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE status = 'sweep_eligible'
              AND treasury_tx_hash IS NULL
            ORDER BY sweep_eligible_at ASC
            LIMIT @Limit
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetConfirmedPaymentsReadyForSweepEligibilityAsync(
        int refundWindowDays = 14,
        int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                buyer_email AS BuyerEmail,
                sweep_eligible_at AS SweepEligibleAt,
                treasury_tx_hash AS TreasuryTxHash,
                swept_at AS SweptAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE status = 'confirmed'
              AND confirmed_at IS NOT NULL
              AND confirmed_at <= @EligibilityThreshold
            ORDER BY confirmed_at ASC
            LIMIT @Limit
            """;

        var eligibilityThreshold = DateTime.UtcNow.AddDays(-refundWindowDays);

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Limit = limit, EligibilityThreshold = eligibilityThreshold }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task MarkSweepEligibleAsync(Guid paymentId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET status = 'sweep_eligible',
                sweep_eligible_at = @SweepEligibleAt
            WHERE id = @Id AND status = 'confirmed'
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId, SweepEligibleAt = DateTime.UtcNow }, cancellationToken: ct));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Payment cannot be marked as sweep-eligible. Either not confirmed or not found.");
    }

    public async Task MarkSweptAsync(Guid paymentId, string treasuryTxHash, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE payments
            SET status = 'swept',
                treasury_tx_hash = @TreasuryTxHash,
                swept_at = @SweptAt
            WHERE id = @Id
              AND status = 'sweep_eligible'
              AND treasury_tx_hash IS NULL
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = paymentId, TreasuryTxHash = treasuryTxHash, SweptAt = DateTime.UtcNow }, cancellationToken: ct));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Payment cannot be marked as swept. Either not sweep-eligible, already swept, or not found.");
    }

    public async Task<IReadOnlyList<Payment>> GetByIdsAsync(IReadOnlyList<Guid> paymentIds, CancellationToken ct = default)
    {
        if (paymentIds.Count == 0)
            return Array.Empty<Payment>();

        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                buyer_email AS BuyerEmail,
                sweep_eligible_at AS SweepEligibleAt,
                treasury_tx_hash AS TreasuryTxHash,
                swept_at AS SweptAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            WHERE id = ANY(@Ids)
            """;

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new { Ids = paymentIds.ToArray() }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetAllPaymentsForAdminAsync(
        int limit = 100,
        int offset = 0,
        PaymentStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                bird_id AS BirdId,
                purpose AS Purpose,
                amount_cents AS AmountCents,
                currency AS Currency,
                provider AS Provider,
                provider_ref AS ProviderRef,
                status AS Status,
                created_at AS CreatedAt,
                confirmed_at AS ConfirmedAt,
                destination_address AS DestinationAddress,
                derivation_index AS DerivationIndex,
                expires_at AS ExpiresAt,
                claimed_at AS ClaimedAt,
                buyer_email AS BuyerEmail,
                sweep_eligible_at AS SweepEligibleAt,
                treasury_tx_hash AS TreasuryTxHash,
                swept_at AS SweptAt,
                wihngo_amount_cents AS WihngoAmountCents
            FROM payments
            """;

        if (statusFilter.HasValue)
            sql += " WHERE status = @Status";

        sql += " ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset";

        using var connection = await _db.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<PaymentRow>(
            new CommandDefinition(sql, new
            {
                Limit = limit,
                Offset = offset,
                Status = statusFilter.HasValue ? StatusToDb(statusFilter.Value) : null
            }, cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<int> CountAllPaymentsAsync(PaymentStatus? statusFilter = null, CancellationToken ct = default)
    {
        var sql = "SELECT COUNT(*) FROM payments";

        if (statusFilter.HasValue)
            sql += " WHERE status = @Status";

        using var connection = await _db.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Status = statusFilter.HasValue ? StatusToDb(statusFilter.Value) : null }, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------------------

    private static string PurposeToDb(PaymentPurpose purpose) => purpose switch
    {
        PaymentPurpose.BirdSupport => "BIRD_SUPPORT",
        PaymentPurpose.Payout => "PAYOUT",
        PaymentPurpose.Refund => "REFUND",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    private static string ProviderToDb(PaymentProvider provider) => provider switch
    {
        PaymentProvider.UsdcSolana => "USDC_SOLANA",
        PaymentProvider.Stripe => "STRIPE",
        PaymentProvider.PayPal => "PAYPAL",
        PaymentProvider.Manual => "MANUAL",
        PaymentProvider.ManualUsdcSolana => "MANUAL_USDC_SOLANA",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    private static string StatusToDb(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "pending",
        PaymentStatus.Confirmed => "confirmed",
        PaymentStatus.Failed => "failed",
        PaymentStatus.Expired => "expired",
        PaymentStatus.SweepEligible => "sweep_eligible",
        PaymentStatus.Swept => "swept",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    /// <summary>
    /// Internal row class for Dapper mapping.
    /// </summary>
    private sealed class PaymentRow
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? BirdId { get; set; }
        public string Purpose { get; set; } = null!;
        public int AmountCents { get; set; }
        public string Currency { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public string? ProviderRef { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? DestinationAddress { get; set; }
        public long? DerivationIndex { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public string? BuyerEmail { get; set; }
        public DateTime? SweepEligibleAt { get; set; }
        public string? TreasuryTxHash { get; set; }
        public DateTime? SweptAt { get; set; }
        public int WihngoAmountCents { get; set; }

        public Payment ToEntity()
        {
            // Use reflection to set private properties since Payment is immutable
            var payment = (Payment)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Payment));

            typeof(Payment).GetProperty(nameof(Payment.Id))!.SetValue(payment, Id);
            typeof(Payment).GetProperty(nameof(Payment.UserId))!.SetValue(payment, UserId);
            typeof(Payment).GetProperty(nameof(Payment.BirdId))!.SetValue(payment, BirdId);
            typeof(Payment).GetProperty(nameof(Payment.Purpose))!.SetValue(payment, ParsePurpose(Purpose));
            typeof(Payment).GetProperty(nameof(Payment.AmountCents))!.SetValue(payment, AmountCents);
            typeof(Payment).GetProperty(nameof(Payment.Currency))!.SetValue(payment, Currency.Trim());
            typeof(Payment).GetProperty(nameof(Payment.Provider))!.SetValue(payment, ParseProvider(Provider));
            typeof(Payment).GetProperty(nameof(Payment.ProviderRef))!.SetValue(payment, ProviderRef);
            typeof(Payment).GetProperty(nameof(Payment.Status))!.SetValue(payment, ParseStatus(Status));
            typeof(Payment).GetProperty(nameof(Payment.CreatedAt))!.SetValue(payment, CreatedAt);
            typeof(Payment).GetProperty(nameof(Payment.ConfirmedAt))!.SetValue(payment, ConfirmedAt);
            typeof(Payment).GetProperty(nameof(Payment.DestinationAddress))!.SetValue(payment, DestinationAddress);
            typeof(Payment).GetProperty(nameof(Payment.DerivationIndex))!.SetValue(payment, DerivationIndex);
            typeof(Payment).GetProperty(nameof(Payment.ExpiresAt))!.SetValue(payment, ExpiresAt);
            typeof(Payment).GetProperty(nameof(Payment.ClaimedAt))!.SetValue(payment, ClaimedAt);
            typeof(Payment).GetProperty(nameof(Payment.BuyerEmail))!.SetValue(payment, BuyerEmail);
            typeof(Payment).GetProperty(nameof(Payment.SweepEligibleAt))!.SetValue(payment, SweepEligibleAt);
            typeof(Payment).GetProperty(nameof(Payment.TreasuryTxHash))!.SetValue(payment, TreasuryTxHash);
            typeof(Payment).GetProperty(nameof(Payment.SweptAt))!.SetValue(payment, SweptAt);
            typeof(Payment).GetProperty(nameof(Payment.WihngoAmountCents))!.SetValue(payment, WihngoAmountCents);

            return payment;
        }

        private static PaymentPurpose ParsePurpose(string purpose) => purpose switch
        {
            "BIRD_SUPPORT" => PaymentPurpose.BirdSupport,
            "PAYOUT" => PaymentPurpose.Payout,
            "REFUND" => PaymentPurpose.Refund,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };

        private static PaymentProvider ParseProvider(string provider) => provider switch
        {
            "USDC_SOLANA" => PaymentProvider.UsdcSolana,
            "STRIPE" => PaymentProvider.Stripe,
            "PAYPAL" => PaymentProvider.PayPal,
            "MANUAL" => PaymentProvider.Manual,
            "MANUAL_USDC_SOLANA" => PaymentProvider.ManualUsdcSolana,
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        private static PaymentStatus ParseStatus(string status) => status switch
        {
            "pending" => PaymentStatus.Pending,
            "confirmed" => PaymentStatus.Confirmed,
            "failed" => PaymentStatus.Failed,
            "expired" => PaymentStatus.Expired,
            "sweep_eligible" => PaymentStatus.SweepEligible,
            "swept" => PaymentStatus.Swept,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }
}
