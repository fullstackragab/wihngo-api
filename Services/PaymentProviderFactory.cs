using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Factory for resolving payment providers by type.
/// Uses DI container to get provider instances.
/// </summary>
public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentProviderFactory> _logger;

    public PaymentProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<PaymentProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IPaymentProvider GetProvider(PaymentProvider providerType)
    {
        IPaymentProvider? provider = providerType switch
        {
            PaymentProvider.UsdcSolana => _serviceProvider.GetService<UsdcSolanaPaymentProvider>(),
            PaymentProvider.ManualUsdcSolana => _serviceProvider.GetService<ManualUsdcPaymentProvider>(),
            // Stripe and PayPal can be added later
            _ => null
        };

        if (provider is null)
        {
            _logger.LogError("Payment provider {ProviderType} is not configured", providerType);
            throw new NotSupportedException($"Payment provider {providerType} is not configured.");
        }

        return provider;
    }
}

/// <summary>
/// Manual USDC payment provider for mobile-friendly flows.
/// Uses HD-derived addresses with background monitoring.
/// </summary>
public sealed class ManualUsdcPaymentProvider : IPaymentProvider
{
    private readonly ILogger<ManualUsdcPaymentProvider> _logger;

    public PaymentProvider ProviderType => PaymentProvider.ManualUsdcSolana;

    public ManualUsdcPaymentProvider(ILogger<ManualUsdcPaymentProvider> logger)
    {
        _logger = logger;
    }

    public Task<PaymentIntentResult> CreateIntentAsync(
        CreatePaymentIntentCommand command,
        CancellationToken ct = default)
    {
        // Manual payments use HD-derived addresses created by PaymentService.
        // This provider doesn't create intents directly.
        throw new NotSupportedException("Manual payment intents are created through PaymentService.CreateManualPaymentIntentAsync");
    }

    public Task<PaymentVerificationResult> VerifyAsync(
        VerifyPaymentCommand command,
        CancellationToken ct = default)
    {
        // Manual payments are verified by the background monitor service.
        // This is a no-op verification that always succeeds.
        // The actual verification happens in ManualPaymentMonitorService.
        _logger.LogDebug("Manual payment verification for {PaymentId} - delegated to monitor service", command.PaymentId);

        return Task.FromResult(PaymentVerificationResult.Success(
            senderWallet: "monitor-verified",
            txHash: command.ProviderRef,
            blockTime: DateTime.UtcNow,
            verifiedAmountCents: 0 // Amount verified by monitor
        ));
    }
}
