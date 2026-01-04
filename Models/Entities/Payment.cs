using Wihngo.Models.Enums;

namespace Wihngo.Models.Entities;

/// <summary>
/// Provider-agnostic payment record.
/// This is the source of truth for all money movement in the platform.
///
/// The platform does not care HOW money is paid (USDC, Stripe, PayPal).
/// It only reacts to payment EVENTS (confirmed, failed).
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; }

    /// <summary>
    /// User ID. NULL for anonymous manual payments until claimed.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Bird ID for BIRD_SUPPORT payments.
    /// Null for other payment purposes.
    /// </summary>
    public Guid? BirdId { get; private set; }

    /// <summary>
    /// Purpose of this payment.
    /// </summary>
    public PaymentPurpose Purpose { get; private set; }

    /// <summary>
    /// Amount in smallest currency unit (cents for USD).
    /// </summary>
    public int AmountCents { get; private set; }

    /// <summary>
    /// Currency code. Always USD internally.
    /// USDC is just an implementation detail.
    /// </summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>
    /// Payment provider/adapter used.
    /// </summary>
    public PaymentProvider Provider { get; private set; }

    /// <summary>
    /// External reference from provider.
    /// For USDC: transaction hash
    /// For Stripe: payment_intent ID
    /// Must be unique to prevent double-spend.
    /// </summary>
    public string? ProviderRef { get; private set; }

    /// <summary>
    /// Payment lifecycle status.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// When the payment was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the payment was confirmed (if successful).
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// Destination address for manual crypto payments.
    /// Derived from HD wallet at DerivationIndex.
    /// </summary>
    public string? DestinationAddress { get; private set; }

    /// <summary>
    /// HD derivation index used to generate DestinationAddress.
    /// </summary>
    public long? DerivationIndex { get; private set; }

    /// <summary>
    /// When this payment intent expires (for manual payments).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// When the payment was claimed by a user.
    /// </summary>
    public DateTime? ClaimedAt { get; private set; }

    /// <summary>
    /// Email address for payment confirmation.
    /// Required for anonymous manual payments to send claim link.
    /// </summary>
    public string? BuyerEmail { get; private set; }

    /// <summary>
    /// When the payment becomes eligible for sweep (ConfirmedAt + 14 days).
    /// </summary>
    public DateTime? SweepEligibleAt { get; private set; }

    /// <summary>
    /// Transaction hash of the sweep to treasury wallet.
    /// </summary>
    public string? TreasuryTxHash { get; private set; }

    /// <summary>
    /// When the payment was swept to treasury.
    /// </summary>
    public DateTime? SweptAt { get; private set; }

    /// <summary>
    /// Optional additional amount to support Wihngo platform (in cents).
    /// </summary>
    public int WihngoAmountCents { get; private set; }

    private Payment() { } // For Dapper

    /// <summary>
    /// Create a new pending payment (authenticated flow).
    /// </summary>
    /// <param name="birdId">Required for BIRD_SUPPORT, null for other purposes.</param>
    /// <param name="wihngoAmountCents">Optional additional Wihngo support amount.</param>
    public static Payment CreatePending(
        Guid userId,
        PaymentPurpose purpose,
        int amountCents,
        PaymentProvider provider,
        Guid? birdId = null,
        int wihngoAmountCents = 0)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (amountCents <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amountCents));
        if (purpose == PaymentPurpose.BirdSupport && birdId == null)
            throw new ArgumentException("BirdId is required for bird support payments.", nameof(birdId));

        return new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BirdId = birdId,
            Purpose = purpose,
            AmountCents = amountCents,
            Currency = "USD",
            Provider = provider,
            ProviderRef = null,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ConfirmedAt = null,
            WihngoAmountCents = wihngoAmountCents
        };
    }

    /// <summary>
    /// Claim an anonymous payment for a user.
    /// Called after manual payment is confirmed and user logs in.
    /// </summary>
    public void Claim(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (ClaimedAt.HasValue)
            throw new InvalidOperationException("Payment is already claimed.");
        if (Status != PaymentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed payments can be claimed.");

        UserId = userId;
        ClaimedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirm the payment with the provider reference.
    /// </summary>
    /// <param name="providerRef">External reference (tx hash, Stripe ID, etc.)</param>
    public void Confirm(string providerRef)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be confirmed.");
        if (string.IsNullOrWhiteSpace(providerRef))
            throw new ArgumentException("Provider reference is required.", nameof(providerRef));

        ProviderRef = providerRef;
        Status = PaymentStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark the payment as failed.
    /// </summary>
    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be marked as failed.");

        Status = PaymentStatus.Failed;
    }

    /// <summary>
    /// Whether this payment is confirmed and successful.
    /// </summary>
    public bool IsConfirmed => Status == PaymentStatus.Confirmed;

    /// <summary>
    /// Whether this payment is still pending.
    /// </summary>
    public bool IsPending => Status == PaymentStatus.Pending;

    /// <summary>
    /// Whether this payment has expired.
    /// </summary>
    public bool IsExpired => Status == PaymentStatus.Expired;

    /// <summary>
    /// Create a pending manual payment with HD-derived destination address.
    /// Anonymous (UserId = null) until claimed after confirmation.
    /// </summary>
    public static Payment CreatePendingManual(
        PaymentPurpose purpose,
        int amountCents,
        string destinationAddress,
        long derivationIndex,
        DateTime expiresAt,
        string buyerEmail,
        Guid? birdId = null,
        int wihngoAmountCents = 0)
    {
        if (string.IsNullOrWhiteSpace(destinationAddress))
            throw new ArgumentException("Destination address is required.", nameof(destinationAddress));
        if (amountCents <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amountCents));
        if (purpose == PaymentPurpose.BirdSupport && birdId == null)
            throw new ArgumentException("BirdId is required for bird support payments.", nameof(birdId));
        if (string.IsNullOrWhiteSpace(buyerEmail))
            throw new ArgumentException("Email is required for manual payments.", nameof(buyerEmail));

        return new Payment
        {
            Id = Guid.NewGuid(),
            UserId = null, // Anonymous until claimed
            BirdId = birdId,
            Purpose = purpose,
            AmountCents = amountCents,
            Currency = "USD",
            Provider = PaymentProvider.ManualUsdcSolana,
            ProviderRef = null,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ConfirmedAt = null,
            DestinationAddress = destinationAddress,
            DerivationIndex = derivationIndex,
            ExpiresAt = expiresAt,
            BuyerEmail = buyerEmail.Trim().ToLowerInvariant(),
            WihngoAmountCents = wihngoAmountCents
        };
    }

    /// <summary>
    /// Mark the payment as expired.
    /// Used when a manual payment intent times out without receiving funds.
    /// </summary>
    public void Expire()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can expire.");

        Status = PaymentStatus.Expired;
    }

    /// <summary>
    /// Mark the payment as eligible for sweep.
    /// Called when ConfirmedAt + 14 days has passed.
    /// </summary>
    public void MarkSweepEligible()
    {
        if (Status != PaymentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed payments can become sweep-eligible.");
        if (!ConfirmedAt.HasValue)
            throw new InvalidOperationException("Payment must have a confirmed timestamp.");

        Status = PaymentStatus.SweepEligible;
        SweepEligibleAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark the payment as swept to treasury.
    /// </summary>
    /// <param name="treasuryTxHash">The Solana transaction hash of the sweep.</param>
    public void Sweep(string treasuryTxHash)
    {
        if (Status != PaymentStatus.SweepEligible)
            throw new InvalidOperationException("Only sweep-eligible payments can be swept.");
        if (string.IsNullOrWhiteSpace(treasuryTxHash))
            throw new ArgumentException("Treasury transaction hash is required.", nameof(treasuryTxHash));
        if (TreasuryTxHash != null)
            throw new InvalidOperationException("Payment has already been swept.");

        TreasuryTxHash = treasuryTxHash;
        Status = PaymentStatus.Swept;
        SweptAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Whether this payment is eligible for sweep (14 days passed since confirmation).
    /// </summary>
    public bool IsSweepEligible => Status == PaymentStatus.SweepEligible;

    /// <summary>
    /// Whether this payment has been swept to treasury.
    /// </summary>
    public bool IsSwept => Status == PaymentStatus.Swept;

    /// <summary>
    /// Whether this payment can be refunded.
    /// Refunds are only possible before sweep.
    /// </summary>
    public bool CanRefund => Status is PaymentStatus.Confirmed or PaymentStatus.SweepEligible;

    /// <summary>
    /// Calculate when this payment will become sweep-eligible.
    /// Returns null if already swept or not confirmed.
    /// </summary>
    public DateTime? CalculateSweepEligibleAt(int refundWindowDays = 14)
    {
        if (!ConfirmedAt.HasValue || IsSwept)
            return null;

        return ConfirmedAt.Value.AddDays(refundWindowDays);
    }

    /// <summary>
    /// Check if the 14-day refund window has passed and payment is ready for sweep eligibility.
    /// </summary>
    public bool IsReadyForSweepEligibility(int refundWindowDays = 14)
    {
        if (Status != PaymentStatus.Confirmed || !ConfirmedAt.HasValue)
            return false;

        return DateTime.UtcNow >= ConfirmedAt.Value.AddDays(refundWindowDays);
    }

    /// <summary>
    /// Total amount including Wihngo support.
    /// </summary>
    public int TotalAmountCents => AmountCents + WihngoAmountCents;
}
