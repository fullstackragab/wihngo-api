using Wihngo.Models.Enums;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Payment service for orchestrating payment flows.
/// This is the main entry point for all payment operations.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create a new payment intent.
    /// Creates a pending payment record and returns destination details.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid userId,
        PaymentPurpose purpose,
        int amountCents,
        PaymentProvider provider,
        Guid? birdId = null,
        int wihngoAmountCents = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Submit and verify a payment.
    /// Verifies the transaction with the provider and confirms the payment.
    /// Emits PaymentConfirmed or PaymentFailed event.
    /// </summary>
    Task<PaymentVerificationResult> SubmitPaymentAsync(
        Guid paymentId,
        string providerRef,
        CancellationToken ct = default);

    /// <summary>
    /// Get payment status by ID.
    /// </summary>
    Task<PaymentStatusResult?> GetPaymentStatusAsync(
        Guid paymentId,
        CancellationToken ct = default);

    /// <summary>
    /// Recover orphaned payments - confirmed payments that are missing support records.
    /// This can happen if the event handler fails after payment confirmation.
    /// Returns the number of support records created.
    /// </summary>
    Task<int> RecoverOrphanedPaymentsAsync(
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Create a manual payment intent with HD-derived destination address.
    /// Anonymous until claimed after confirmation.
    /// </summary>
    /// <param name="buyerEmail">Email address for sending payment confirmation with claim link.</param>
    Task<ManualPaymentIntentResult> CreateManualPaymentIntentAsync(
        PaymentPurpose purpose,
        int amountCents,
        string buyerEmail,
        Guid? birdId = null,
        int wihngoAmountCents = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Claim a confirmed anonymous payment for a user.
    /// Grants bird support access after successful claim.
    /// </summary>
    Task<ClaimPaymentResult> ClaimPaymentAsync(
        Guid paymentId,
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>
/// Result of creating a payment intent.
/// </summary>
public sealed record PaymentIntentResult(
    Guid PaymentId,
    int AmountCents,
    string DestinationWallet,
    string TokenMint,
    DateTime ExpiresAt
)
{
    public static PaymentIntentResult Failed(string reason) =>
        new(Guid.Empty, 0, string.Empty, string.Empty, DateTime.MinValue)
        {
            IsSuccess = false,
            FailureReason = reason
        };

    public bool IsSuccess { get; init; } = true;
    public string? FailureReason { get; init; }
}

/// <summary>
/// Result of verifying a payment.
/// </summary>
public sealed record PaymentVerificationResult(
    bool IsValid,
    string? FailureReason = null,
    string? SenderWallet = null,
    string? TxHash = null,
    DateTime? BlockTime = null,
    int VerifiedAmountCents = 0
)
{
    public static PaymentVerificationResult Success(
        string senderWallet,
        string txHash,
        DateTime blockTime,
        int verifiedAmountCents) =>
        new(true, null, senderWallet, txHash, blockTime, verifiedAmountCents);

    public static PaymentVerificationResult Invalid(string reason) =>
        new(false, reason);
}

/// <summary>
/// Result of a payment status query.
/// </summary>
public sealed record PaymentStatusResult(
    Guid PaymentId,
    PaymentStatus Status,
    PaymentPurpose Purpose,
    Guid? BirdId,
    int AmountCents,
    PaymentProvider Provider,
    string? ProviderRef,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    /// <summary>Whether this is a manual payment requiring claim step.</summary>
    bool IsManualPayment = false,
    /// <summary>Whether the payment has been claimed by a user (manual payments only).</summary>
    bool IsClaimed = false,
    /// <summary>When the payment was claimed (manual payments only).</summary>
    DateTime? ClaimedAt = null,
    /// <summary>Whether claim is required to receive access (confirmed but unclaimed manual payment).</summary>
    bool ClaimRequired = false,
    /// <summary>Optional Wihngo support amount.</summary>
    int WihngoAmountCents = 0
);

/// <summary>
/// Result of creating a manual payment intent.
/// </summary>
public sealed record ManualPaymentIntentResult(
    Guid PaymentId,
    int AmountCents,
    string Currency,
    string Network,
    string DestinationAddress,
    DateTime ExpiresAt
)
{
    public static ManualPaymentIntentResult Failed(string reason) =>
        new(Guid.Empty, 0, string.Empty, string.Empty, string.Empty, DateTime.MinValue)
        {
            IsSuccess = false,
            FailureReason = reason
        };

    public bool IsSuccess { get; init; } = true;
    public string? FailureReason { get; init; }
}

/// <summary>
/// Result of claiming an anonymous payment.
/// </summary>
public sealed record ClaimPaymentResult(
    bool IsSuccess,
    string? FailureReason = null,
    Guid? BirdId = null
)
{
    public static ClaimPaymentResult Success(Guid? birdId) => new(true, null, birdId);
    public static ClaimPaymentResult Failed(string reason) => new(false, reason);
}
