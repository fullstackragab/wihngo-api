using Wihngo.Models.Enums;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Payment provider abstraction.
/// Implementations handle the specifics of each payment method (USDC, Stripe, etc.)
/// while the platform remains agnostic to how money is actually moved.
///
/// Rules:
/// - Providers MUST be stateless
/// - Providers MUST NOT write to the database
/// - Providers MUST NOT implement business logic (no balances, no access, no settlements)
/// - Providers only handle payment verification and intent creation
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// The provider type this implementation handles.
    /// </summary>
    PaymentProvider ProviderType { get; }

    /// <summary>
    /// Create a payment intent for the user to fulfill.
    /// Returns destination details (wallet address, expected amount, etc.)
    /// </summary>
    /// <param name="command">The payment intent command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Payment intent result with destination details.</returns>
    Task<PaymentIntentResult> CreateIntentAsync(
        CreatePaymentIntentCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Verify that a payment was actually made.
    /// Checks the blockchain/provider to confirm the transaction.
    /// </summary>
    /// <param name="command">The verification command with provider reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Verification result with transaction details.</returns>
    Task<PaymentVerificationResult> VerifyAsync(
        VerifyPaymentCommand command,
        CancellationToken ct = default);
}

/// <summary>
/// Command to create a payment intent.
/// </summary>
public sealed record CreatePaymentIntentCommand(
    Guid UserId,
    PaymentPurpose Purpose,
    int AmountCents,
    Guid? BirdId = null
);

/// <summary>
/// Command to verify a payment.
/// </summary>
public sealed record VerifyPaymentCommand(
    Guid PaymentId,
    string ProviderRef
);

/// <summary>
/// Factory for getting payment providers by type.
/// </summary>
public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProvider providerType);
}
