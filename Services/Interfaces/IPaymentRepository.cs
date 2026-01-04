using Wihngo.Models.Entities;
using Wihngo.Models.Enums;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Repository for payment ledger operations.
/// </summary>
public interface IPaymentRepository
{
    // READ
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetStalePendingPaymentsAsync(TimeSpan olderThan, int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetOrphanedConfirmedPaymentsAsync(int limit = 100, CancellationToken ct = default);

    // WRITE
    Task InsertAsync(Payment payment, CancellationToken ct = default);
    Task ConfirmAsync(Guid paymentId, string providerRef, DateTime confirmedAt, CancellationToken ct = default);
    Task FailAsync(Guid paymentId, CancellationToken ct = default);

    // MANUAL PAYMENT METHODS
    Task InsertManualPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetPendingManualPaymentsAsync(int limit = 100, CancellationToken ct = default);
    Task ExpireAsync(Guid paymentId, CancellationToken ct = default);
    Task ClaimAsync(Guid paymentId, Guid userId, CancellationToken ct = default);

    // ADMIN OPERATIONS
    Task<IReadOnlyList<Payment>> GetUnclaimedConfirmedPaymentsAsync(int limit = 100, int offset = 0, CancellationToken ct = default);
    Task<int> CountUnclaimedConfirmedPaymentsAsync(CancellationToken ct = default);
    Task AdminAssignAsync(Guid paymentId, Guid userId, Guid adminId, string? reason, CancellationToken ct = default);

    // SWEEP OPERATIONS
    Task<IReadOnlyList<Payment>> GetSweepEligiblePaymentsAsync(int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetConfirmedPaymentsReadyForSweepEligibilityAsync(int refundWindowDays = 14, int limit = 100, CancellationToken ct = default);
    Task MarkSweepEligibleAsync(Guid paymentId, CancellationToken ct = default);
    Task MarkSweptAsync(Guid paymentId, string treasuryTxHash, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByIdsAsync(IReadOnlyList<Guid> paymentIds, CancellationToken ct = default);

    // LIST OPERATIONS
    Task<IReadOnlyList<Payment>> GetAllPaymentsForAdminAsync(int limit = 100, int offset = 0, PaymentStatus? statusFilter = null, CancellationToken ct = default);
    Task<int> CountAllPaymentsAsync(PaymentStatus? statusFilter = null, CancellationToken ct = default);
}
