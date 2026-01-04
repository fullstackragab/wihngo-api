namespace Wihngo.Models.Enums;

/// <summary>
/// Lifecycle status of a payment.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment initiated, awaiting confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment confirmed and completed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Payment failed or was declined.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment intent expired without receiving funds.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Payment confirmed and 14-day refund window passed.
    /// Eligible to be swept to treasury.
    /// </summary>
    SweepEligible = 4,

    /// <summary>
    /// Payment has been swept to treasury wallet.
    /// Funds are finalized. Refunds require treasury outflow.
    /// </summary>
    Swept = 5
}

/// <summary>
/// Purpose of a payment in the ledger.
/// </summary>
public enum PaymentPurpose
{
    /// <summary>
    /// User supporting a bird.
    /// </summary>
    BirdSupport = 0,

    /// <summary>
    /// Platform paying out to a bird owner.
    /// </summary>
    Payout = 1,

    /// <summary>
    /// Refund to a user.
    /// </summary>
    Refund = 2,

    /// <summary>
    /// User supporting the Wihngo platform directly (no specific bird).
    /// </summary>
    PlatformSupport = 3
}

/// <summary>
/// Payment provider/adapter.
/// The platform is agnostic to which provider is used.
/// </summary>
public enum PaymentProvider
{
    /// <summary>
    /// USDC on Solana blockchain via Phantom wallet.
    /// </summary>
    UsdcSolana = 0,

    /// <summary>
    /// Stripe payment processing.
    /// </summary>
    Stripe = 1,

    /// <summary>
    /// PayPal payment processing.
    /// </summary>
    PayPal = 2,

    /// <summary>
    /// Manual payment (admin-initiated).
    /// </summary>
    Manual = 3,

    /// <summary>
    /// Manual USDC payment with HD-derived addresses.
    /// Mobile-friendly, no wallet deep-links required.
    /// </summary>
    ManualUsdcSolana = 4
}
