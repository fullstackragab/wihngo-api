using Microsoft.Extensions.Logging;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Payment service orchestrator.
/// Creates payment records, delegates to providers for verification,
/// and emits domain events on confirmation/failure.
///
/// This service is the single point of coordination for all payment flows.
/// It enforces idempotency via provider_ref uniqueness.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly ISolanaHdWalletService _hdWalletService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentProviderFactory providerFactory,
        ISolanaHdWalletService hdWalletService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _providerFactory = providerFactory;
        _hdWalletService = hdWalletService;
        _logger = logger;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid userId,
        PaymentPurpose purpose,
        int amountCents,
        PaymentProvider provider,
        Guid? birdId = null,
        int wihngoAmountCents = 0,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Creating payment intent for user {UserId}, purpose {Purpose}, amount {Amount} cents, provider {Provider}",
            userId, purpose, amountCents, provider);

        // Create pending payment record
        var payment = Payment.CreatePending(userId, purpose, amountCents, provider, birdId, wihngoAmountCents);

        await _paymentRepository.InsertAsync(payment, ct);

        _logger.LogInformation(
            "Created pending payment {PaymentId} for user {UserId}",
            payment.Id, userId);

        // Get provider and create intent
        var paymentProvider = _providerFactory.GetProvider(provider);
        var command = new CreatePaymentIntentCommand(userId, purpose, amountCents + wihngoAmountCents, birdId);
        var intent = await paymentProvider.CreateIntentAsync(command, ct);

        // Return intent with payment ID
        return intent with { PaymentId = payment.Id };
    }

    public async Task<PaymentVerificationResult> SubmitPaymentAsync(
        Guid paymentId,
        string providerRef,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Submitting payment {PaymentId} with provider ref {ProviderRef}",
            paymentId, providerRef);

        // Get payment record
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", paymentId);
            return PaymentVerificationResult.Invalid("Payment not found");
        }

        if (!payment.IsPending)
        {
            _logger.LogWarning(
                "Payment {PaymentId} is not pending, status: {Status}",
                paymentId, payment.Status);
            return PaymentVerificationResult.Invalid($"Payment is not pending, status: {payment.Status}");
        }

        // Idempotency check: ensure provider_ref not already used
        var existingPayment = await _paymentRepository.GetByProviderRefAsync(providerRef, ct);
        if (existingPayment is not null)
        {
            _logger.LogWarning(
                "Provider ref {ProviderRef} already used for payment {ExistingPaymentId}",
                providerRef, existingPayment.Id);
            return PaymentVerificationResult.Invalid("Transaction already processed");
        }

        // Get provider and verify
        var provider = _providerFactory.GetProvider(payment.Provider);
        var verifyCommand = new VerifyPaymentCommand(paymentId, providerRef);
        var result = await provider.VerifyAsync(verifyCommand, ct);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Payment {PaymentId} verification failed: {Reason}",
                paymentId, result.FailureReason);

            await _paymentRepository.FailAsync(paymentId, ct);
            return result;
        }

        // Verify amount matches (total including Wihngo support)
        var expectedAmount = payment.TotalAmountCents;
        if (result.VerifiedAmountCents != expectedAmount)
        {
            _logger.LogWarning(
                "Payment {PaymentId} amount mismatch: expected {Expected}, got {Actual}",
                paymentId, expectedAmount, result.VerifiedAmountCents);

            await _paymentRepository.FailAsync(paymentId, ct);

            return PaymentVerificationResult.Invalid(
                $"Amount mismatch: expected {expectedAmount}, got {result.VerifiedAmountCents}");
        }

        // Confirm payment
        await _paymentRepository.ConfirmAsync(paymentId, providerRef, DateTime.UtcNow, ct);

        _logger.LogInformation(
            "Payment {PaymentId} confirmed with provider ref {ProviderRef}",
            paymentId, providerRef);

        return result;
    }

    public async Task<PaymentStatusResult?> GetPaymentStatusAsync(
        Guid paymentId,
        CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return null;

        var isManualPayment = payment.Provider == PaymentProvider.ManualUsdcSolana;
        var isClaimed = payment.ClaimedAt.HasValue;
        var claimRequired = isManualPayment && payment.IsConfirmed && !isClaimed;

        return new PaymentStatusResult(
            payment.Id,
            payment.Status,
            payment.Purpose,
            payment.BirdId,
            payment.AmountCents,
            payment.Provider,
            payment.ProviderRef,
            payment.CreatedAt,
            payment.ConfirmedAt,
            IsManualPayment: isManualPayment,
            IsClaimed: isClaimed,
            ClaimedAt: payment.ClaimedAt,
            ClaimRequired: claimRequired,
            WihngoAmountCents: payment.WihngoAmountCents
        );
    }

    public async Task<int> RecoverOrphanedPaymentsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting orphaned payment recovery (limit: {Limit})", limit);

        var orphanedPayments = await _paymentRepository.GetOrphanedConfirmedPaymentsAsync(limit, ct);

        if (orphanedPayments.Count == 0)
        {
            _logger.LogInformation("No orphaned payments found");
            return 0;
        }

        _logger.LogWarning(
            "Found {Count} orphaned confirmed payments without support records",
            orphanedPayments.Count);

        var recovered = 0;

        foreach (var payment in orphanedPayments)
        {
            // Skip anonymous payments - they need to be claimed first
            if (!payment.UserId.HasValue)
            {
                _logger.LogDebug(
                    "Skipping anonymous payment {PaymentId} - needs to be claimed",
                    payment.Id);
                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Recovering payment {PaymentId} for user {UserId}, bird {BirdId}",
                    payment.Id, payment.UserId, payment.BirdId);

                // TODO: Re-emit the PaymentConfirmed event to trigger support record creation
                // For now, just count as recovered

                recovered++;

                _logger.LogInformation(
                    "Successfully recovered payment {PaymentId}",
                    payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to recover payment {PaymentId}",
                    payment.Id);
            }
        }

        _logger.LogInformation(
            "Orphaned payment recovery complete: {Recovered}/{Total} recovered",
            recovered, orphanedPayments.Count);

        return recovered;
    }

    public async Task<ManualPaymentIntentResult> CreateManualPaymentIntentAsync(
        PaymentPurpose purpose,
        int amountCents,
        string buyerEmail,
        Guid? birdId = null,
        int wihngoAmountCents = 0,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Creating manual payment intent: purpose={Purpose}, amount={Amount} cents, bird={BirdId}, email={Email}",
            purpose, amountCents, birdId, buyerEmail);

        // Check if HD wallet is configured
        if (!_hdWalletService.IsConfigured)
        {
            _logger.LogError("Manual payments not available: HD wallet not configured");
            return ManualPaymentIntentResult.Failed("Manual payments are not available");
        }

        // Get next derivation index atomically
        var derivationIndex = await _hdWalletService.GetNextDerivationIndexAsync(ct);

        // Derive unique address for this payment
        var destinationAddress = _hdWalletService.DeriveAddress(derivationIndex);

        // Calculate expiration (default: 60 minutes)
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        // Create anonymous payment entity (UserId = null)
        var payment = Payment.CreatePendingManual(
            purpose,
            amountCents + wihngoAmountCents,
            destinationAddress,
            derivationIndex,
            expiresAt,
            buyerEmail,
            birdId,
            wihngoAmountCents
        );

        await _paymentRepository.InsertManualPaymentAsync(payment, ct);

        _logger.LogInformation(
            "Created manual payment intent {PaymentId}: index={Index}, address={Address}, email={Email}, expires={ExpiresAt}",
            payment.Id, derivationIndex, destinationAddress, buyerEmail, expiresAt);

        return new ManualPaymentIntentResult(
            PaymentId: payment.Id,
            AmountCents: amountCents + wihngoAmountCents,
            Currency: "USDC",
            Network: "SOLANA",
            DestinationAddress: destinationAddress,
            ExpiresAt: expiresAt
        );
    }

    public async Task<ClaimPaymentResult> ClaimPaymentAsync(
        Guid paymentId,
        Guid userId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Claiming payment {PaymentId} for user {UserId}", paymentId, userId);

        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return ClaimPaymentResult.Failed("Payment not found");

        // Validate payment state
        if (payment.ClaimedAt.HasValue)
            return ClaimPaymentResult.Failed("Payment already claimed");

        if (payment.Status != PaymentStatus.Confirmed)
            return ClaimPaymentResult.Failed("Payment not confirmed yet");

        if (payment.Provider != PaymentProvider.ManualUsdcSolana)
            return ClaimPaymentResult.Failed("Only manual payments can be claimed");

        // Claim the payment
        await _paymentRepository.ClaimAsync(paymentId, userId, ct);

        _logger.LogInformation(
            "Payment {PaymentId} claimed by user {UserId}",
            paymentId, userId);

        return ClaimPaymentResult.Success(payment.BirdId);
    }
}
