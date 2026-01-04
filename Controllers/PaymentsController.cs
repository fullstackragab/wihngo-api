using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Payment processing for USDC support payments.
/// Handles payment intents, confirmation, and status checks.
/// </summary>
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ISolanaTransactionService _solanaService;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly SolanaConfiguration _solanaSettings;
    private readonly P2PPaymentConfiguration _p2pConfig;
    private readonly PlatformConfiguration _platformSettings;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ISolanaTransactionService solanaService,
        IDbConnectionFactory dbFactory,
        IOptions<SolanaConfiguration> solanaSettings,
        IOptions<P2PPaymentConfiguration> p2pConfig,
        IOptions<PlatformConfiguration> platformSettings,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _solanaService = solanaService;
        _dbFactory = dbFactory;
        _solanaSettings = solanaSettings.Value;
        _p2pConfig = p2pConfig.Value;
        _platformSettings = platformSettings.Value;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get Solana payment configuration.
    /// Returns network info for frontend to verify cluster match.
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaymentConfigResponse), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        var isDevnet = _solanaSettings.RpcUrl?.Contains("devnet") ?? false;
        return Ok(new PaymentConfigResponse(
            Network: isDevnet ? "devnet" : "mainnet-beta",
            RpcUrl: _solanaSettings.RpcUrl ?? "https://api.mainnet-beta.solana.com",
            UsdcMint: _solanaSettings.UsdcMintAddress,
            PlatformWallet: _solanaSettings.PlatformWalletAddress
        ));
    }

    /// <summary>
    /// Create a payment intent for bird support.
    /// Returns the destination wallet and amount for USDC payment.
    /// </summary>
    /// <param name="request">Payment intent request with bird ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Payment intent with destination details.</returns>
    [HttpPost("intents")]
    [ProducesResponseType(typeof(CreatePaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntent(
        [FromBody] BirdSupportPaymentRequest request,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User not authenticated." });

        if (request.AmountCents <= 0)
            return BadRequest(new { error = "Amount must be positive." });

        // Create payment intent (currently only USDC on Solana)
        var result = await _paymentService.CreatePaymentIntentAsync(
            userId: userId,
            purpose: PaymentPurpose.BirdSupport,
            amountCents: request.AmountCents,
            provider: PaymentProvider.UsdcSolana,
            birdId: request.BirdId,
            wihngoAmountCents: request.WihngoAmountCents,
            ct: ct
        );

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "[PAYMENT] Intent failed | UserId={UserId} | BirdId={BirdId} | Reason={Reason}",
                userId, request.BirdId, result.FailureReason);
            return BadRequest(new { error = result.FailureReason });
        }

        // Build return URL for mobile Phantom deep-link flow
        var frontendUrl = _platformSettings.FrontendUrl?.TrimEnd('/') ?? "https://wihngo.com";
        var returnUrl = $"{frontendUrl}/payments/return?pi={result.PaymentId}";

        _logger.LogInformation(
            "[PAYMENT] Intent created | PaymentId={PaymentId} | UserId={UserId} | BirdId={BirdId} | Amount=${Amount} | Wallet={Wallet}",
            result.PaymentId, userId, request.BirdId, result.AmountCents / 100.0m, result.DestinationWallet);

        return Ok(new CreatePaymentIntentResponse(
            PaymentId: result.PaymentId,
            AmountCents: result.AmountCents,
            Currency: "USD",
            DestinationWallet: result.DestinationWallet,
            TokenMint: result.TokenMint,
            ExpiresAt: result.ExpiresAt,
            ReturnUrl: returnUrl
        ));
    }

    /// <summary>
    /// Confirm a payment with transaction hash.
    /// Verifies the blockchain transaction and grants access on success.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User not authenticated." });

        if (string.IsNullOrWhiteSpace(request.TxHash))
            return BadRequest(new { error = "Transaction hash is required." });

        // Verify the payment
        var result = await _paymentService.SubmitPaymentAsync(request.PaymentId, request.TxHash, ct);

        // Get updated status
        var status = await _paymentService.GetPaymentStatusAsync(request.PaymentId, ct);
        if (status is null)
            return NotFound(new { error = "Payment not found." });

        if (result.IsValid)
        {
            _logger.LogInformation(
                "[PAYMENT] Confirmed | PaymentId={PaymentId} | UserId={UserId} | BirdId={BirdId} | Amount=${Amount} | TxHash={TxHash}",
                request.PaymentId, userId, status.BirdId, status.AmountCents / 100.0m, request.TxHash);
        }
        else
        {
            _logger.LogWarning(
                "[PAYMENT] Verification failed | PaymentId={PaymentId} | UserId={UserId} | TxHash={TxHash} | Reason={Reason}",
                request.PaymentId, userId, request.TxHash, result.FailureReason);
        }

        return Ok(new ConfirmPaymentResponse(
            PaymentId: status.PaymentId,
            Status: StatusToString(status.Status),
            IsSuccess: result.IsValid,
            FailureReason: result.FailureReason,
            ConfirmedAt: status.ConfirmedAt
        ));
    }

    /// <summary>
    /// Get payment status by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(Guid id, CancellationToken ct = default)
    {
        var status = await _paymentService.GetPaymentStatusAsync(id, ct);
        if (status is null)
            return NotFound(new { error = "Payment not found." });

        return Ok(new PaymentStatusResponse(
            PaymentId: status.PaymentId,
            Status: StatusToString(status.Status),
            Purpose: PurposeToString(status.Purpose),
            BirdId: status.BirdId,
            AmountCents: status.AmountCents,
            Currency: "USD",
            Provider: ProviderToString(status.Provider),
            CreatedAt: status.CreatedAt,
            ConfirmedAt: status.ConfirmedAt
        ));
    }

    /// <summary>
    /// Get payment intent status (simplified).
    /// </summary>
    [HttpGet("intents/{id:guid}/status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IntentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntentStatus(Guid id, CancellationToken ct = default)
    {
        var status = await _paymentService.GetPaymentStatusAsync(id, ct);
        if (status is null)
            return NotFound(new { error = "Intent not found." });

        // Build claim messaging for confirmed manual payments
        string? message = null;
        string? claimUrl = null;
        var frontendUrl = _platformSettings.FrontendUrl?.TrimEnd('/') ?? "https://wihngo.com";

        if (status.ClaimRequired)
        {
            claimUrl = $"{frontendUrl}/payments/claim?pi={id}";
            message = "Payment confirmed. You must claim your support to complete. Save this link!";
        }
        else if (status.IsManualPayment && status.IsClaimed)
        {
            message = "Payment claimed. Support completed.";
        }

        return Ok(new IntentStatusResponse(
            PaymentId: status.PaymentId.ToString(),
            Status: StatusToIntentStatus(status.Status),
            BirdId: status.BirdId?.ToString(),
            BirdName: null, // Could fetch bird name if needed
            AmountCents: status.AmountCents,
            Message: message,
            ClaimRequired: status.ClaimRequired,
            ClaimUrl: claimUrl
        ));
    }

    /// <summary>
    /// Create a manual payment intent for mobile support.
    /// Returns a unique destination address for USDC payment.
    /// </summary>
    [HttpPost("manual")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateManualPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateManualPaymentIntent(
        [FromBody] CreateManualPaymentRequest request,
        CancellationToken ct = default)
    {
        if (request.AmountCents <= 0)
            return BadRequest(new { error = "Amount must be positive." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required for manual payments." });

        // Create anonymous payment intent (user claims after confirmation)
        var result = await _paymentService.CreateManualPaymentIntentAsync(
            purpose: PaymentPurpose.BirdSupport,
            amountCents: request.AmountCents,
            buyerEmail: request.Email,
            birdId: request.BirdId,
            wihngoAmountCents: request.WihngoAmountCents,
            ct: ct
        );

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "[PAYMENT] Manual intent failed | BirdId={BirdId} | Reason={Reason}",
                request.BirdId, result.FailureReason);
            return BadRequest(new { error = result.FailureReason });
        }

        _logger.LogInformation(
            "[PAYMENT] Manual intent created | PaymentId={PaymentId} | BirdId={BirdId} | Amount=${Amount} | Address={Address} | ExpiresAt={ExpiresAt}",
            result.PaymentId, request.BirdId, result.AmountCents / 100.0m, result.DestinationAddress, result.ExpiresAt);

        // Build authoritative claim URL
        var frontendUrl = _platformSettings.FrontendUrl?.TrimEnd('/') ?? "https://wihngo.com";
        var claimUrl = $"{frontendUrl}/payments/claim?pi={result.PaymentId}";

        return Ok(new CreateManualPaymentResponse(
            PaymentId: result.PaymentId,
            AmountCents: result.AmountCents,
            Currency: result.Currency,
            Network: result.Network,
            DestinationAddress: result.DestinationAddress,
            ExpiresAt: result.ExpiresAt,
            ClaimUrl: claimUrl,
            Message: "After payment confirms, you must claim your support using the claim URL. Save this link!"
        ));
    }

    /// <summary>
    /// Claim a confirmed anonymous payment.
    /// Attaches the payment to the authenticated user and grants access.
    /// </summary>
    [HttpPost("{id:guid}/claim")]
    [ProducesResponseType(typeof(ClaimPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClaimPayment(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User not authenticated." });

        var result = await _paymentService.ClaimPaymentAsync(id, userId, ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "[PAYMENT] Claim failed | PaymentId={PaymentId} | UserId={UserId} | Reason={Reason}",
                id, userId, result.FailureReason);

            if (result.FailureReason == "Payment not found")
                return NotFound(new { error = result.FailureReason });

            return BadRequest(new { error = result.FailureReason });
        }

        _logger.LogInformation(
            "[PAYMENT] Claimed | PaymentId={PaymentId} | UserId={UserId} | BirdId={BirdId}",
            id, userId, result.BirdId);

        return Ok(new ClaimPaymentResponse(
            PaymentId: id,
            BirdId: result.BirdId,
            Message: "Payment claimed successfully. Support completed."
        ));
    }

    /// <summary>
    /// Verify an externally-submitted Solana transaction containing two USDC transfers.
    /// This endpoint validates that a transaction contains the expected split payment
    /// (bird owner amount + Wihngo platform amount) and records it as confirmed.
    /// </summary>
    /// <remarks>
    /// Use this endpoint when the transaction was submitted externally (not through our system).
    /// The transaction must:
    /// - Be finalized on Solana
    /// - Contain exactly two USDC SPL token transfers
    /// - One transfer to the bird owner's wallet with the expected amount
    /// - One transfer to the Wihngo treasury wallet with the expected amount
    /// - Have the authenticated user's wallet as the payer
    ///
    /// Amount conversion: cents to USDC uses 6 decimals
    /// - 500 cents = $5.00 = 5.00 USDC = 5,000,000 raw units
    /// </remarks>
    [HttpPost("solana/verify")]
    [ProducesResponseType(typeof(VerifySolanaSupportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifySolanaSupport(
        [FromBody] VerifySolanaSupportRequest request,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User not authenticated." });

        if (!ModelState.IsValid)
        {
            return BadRequest(new VerifySolanaSupportResponse
            {
                Success = false,
                Status = "failed",
                TxHash = request.TxHash,
                Error = "Validation failed"
            });
        }

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Check if txHash already used (idempotency/replay protection)
        var existingPayment = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, status FROM support_intents
              WHERE solana_signature = @TxHash
              LIMIT 1",
            new { request.TxHash });

        if (existingPayment != null)
        {
            _logger.LogWarning(
                "[PAYMENT] Transaction already used: {TxHash}, existing payment: {PaymentId}",
                request.TxHash, existingPayment.id);

            return BadRequest(new VerifySolanaSupportResponse
            {
                Success = false,
                Status = "failed",
                TxHash = request.TxHash,
                PaymentId = existingPayment.id,
                Error = "Transaction hash already used"
            });
        }

        // 2. Get bird info
        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT bird_id, owner_id, name FROM birds WHERE bird_id = @BirdId",
            new { request.BirdId });

        if (bird == null)
        {
            return BadRequest(new VerifySolanaSupportResponse
            {
                Success = false,
                Status = "failed",
                TxHash = request.TxHash,
                Error = "Bird not found"
            });
        }

        // 3. Get user's wallet to verify they are the payer
        var userWallet = await conn.QueryFirstOrDefaultAsync<string>(
            @"SELECT public_key FROM wallets
              WHERE user_id = @UserId AND is_primary = true
              LIMIT 1",
            new { UserId = userId });

        if (string.IsNullOrEmpty(userWallet))
        {
            return BadRequest(new VerifySolanaSupportResponse
            {
                Success = false,
                Status = "failed",
                TxHash = request.TxHash,
                Error = "User has no connected wallet"
            });
        }

        // 4. Convert cents to USDC (USDC has 6 decimals)
        decimal birdAmountUsdc = request.BirdAmountCents / 100.0m;
        decimal wihngoAmountUsdc = request.WihngoAmountCents / 100.0m;

        // 5. Verify the transaction on-chain
        var verificationResult = await _solanaService.VerifyDualTransferTransactionAsync(
            request.TxHash,
            userWallet,
            request.BirdWallet,
            birdAmountUsdc,
            _p2pConfig.WihngoTreasuryWallet,
            wihngoAmountUsdc);

        // Build verification details for response
        var details = new VerificationDetails
        {
            TransactionFound = verificationResult.TransactionFound,
            TransactionSucceeded = verificationResult.TransactionSucceeded,
            MintMatches = verificationResult.MintMatches,
            PayerMatches = verificationResult.PayerMatches,
            UsdcTransferCount = verificationResult.UsdcTransferCount,
            BirdTransferValid = verificationResult.BirdTransfer?.Found == true && verificationResult.BirdTransfer.AmountMatches,
            WihngoTransferValid = verificationResult.WihngoTransfer?.Found == true && verificationResult.WihngoTransfer.AmountMatches,
            ActualBirdAmount = verificationResult.BirdTransfer?.ActualAmount ?? 0,
            ActualWihngoAmount = verificationResult.WihngoTransfer?.ActualAmount ?? 0
        };

        if (!verificationResult.Success)
        {
            _logger.LogWarning(
                "[PAYMENT] Verification failed: TxHash={TxHash}, Error={Error}",
                request.TxHash, verificationResult.Error);

            return BadRequest(new VerifySolanaSupportResponse
            {
                Success = false,
                Status = "failed",
                TxHash = request.TxHash,
                BirdId = request.BirdId,
                BirdName = bird.name,
                Error = verificationResult.Error,
                Details = details
            });
        }

        // 6. Transaction verified - create support intent record
        var intentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(
            @"INSERT INTO support_intents
              (id, supporter_user_id, bird_id, recipient_user_id, support_amount, bird_amount, wihngo_support_amount,
               total_amount, currency, status, payment_method, sender_wallet_pubkey,
               recipient_wallet_pubkey, wihngo_wallet_pubkey, solana_signature, confirmations,
               paid_at, completed_at, created_at, updated_at)
              VALUES (@Id, @SupporterUserId, @BirdId, @RecipientUserId, @BirdAmount, @BirdAmount, @WihngoSupportAmount,
               @TotalAmount, @Currency, @Status, @PaymentMethod, @SenderWalletPubkey,
               @RecipientWalletPubkey, @WihngoWalletPubkey, @SolanaSignature, @Confirmations,
               @PaidAt, @CompletedAt, @CreatedAt, @UpdatedAt)",
            new
            {
                Id = intentId,
                SupporterUserId = userId,
                request.BirdId,
                RecipientUserId = (Guid)bird.owner_id,
                BirdAmount = birdAmountUsdc,
                WihngoSupportAmount = wihngoAmountUsdc,
                TotalAmount = birdAmountUsdc + wihngoAmountUsdc,
                Currency = "USDC",
                Status = "completed",
                PaymentMethod = "wallet",
                SenderWalletPubkey = userWallet,
                RecipientWalletPubkey = request.BirdWallet,
                WihngoWalletPubkey = _p2pConfig.WihngoTreasuryWallet,
                SolanaSignature = request.TxHash,
                Confirmations = 32, // Finalized = 32 confirmations
                PaidAt = now,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

        // 7. Update bird support count
        await conn.ExecuteAsync(
            "UPDATE birds SET supported_count = supported_count + 1 WHERE bird_id = @BirdId",
            new { request.BirdId });

        _logger.LogInformation(
            "[PAYMENT] Verified and confirmed: TxHash={TxHash}, IntentId={IntentId}, BirdId={BirdId}, BirdAmount=${BirdAmount}, WihngoAmount=${WihngoAmount}",
            request.TxHash, intentId, request.BirdId, birdAmountUsdc, wihngoAmountUsdc);

        return Ok(new VerifySolanaSupportResponse
        {
            Success = true,
            Status = "confirmed",
            TxHash = request.TxHash,
            PaymentId = intentId,
            BirdId = request.BirdId,
            BirdName = bird.name,
            PayerWallet = userWallet,
            BirdAmountCents = request.BirdAmountCents,
            WihngoAmountCents = request.WihngoAmountCents,
            Details = details
        });
    }

    private static string StatusToString(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "pending",
        PaymentStatus.Confirmed => "confirmed",
        PaymentStatus.Failed => "failed",
        PaymentStatus.Expired => "expired",
        PaymentStatus.SweepEligible => "sweep_eligible",
        PaymentStatus.Swept => "swept",
        _ => "unknown"
    };

    private static string StatusToIntentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "pending",
        PaymentStatus.Confirmed => "confirmed",
        PaymentStatus.Failed => "failed",
        PaymentStatus.Expired => "expired",
        _ => "pending"
    };

    private static string PurposeToString(PaymentPurpose purpose) => purpose switch
    {
        PaymentPurpose.BirdSupport => "bird_support",
        PaymentPurpose.Payout => "payout",
        PaymentPurpose.Refund => "refund",
        _ => "unknown"
    };

    private static string ProviderToString(PaymentProvider provider) => provider switch
    {
        PaymentProvider.UsdcSolana => "usdc_solana",
        PaymentProvider.Stripe => "stripe",
        PaymentProvider.PayPal => "paypal",
        PaymentProvider.Manual => "manual",
        PaymentProvider.ManualUsdcSolana => "manual_usdc_solana",
        _ => "unknown"
    };
}

// -------------------------------------------------------------------------
// API CONTRACTS
// -------------------------------------------------------------------------

/// <summary>
/// Request to create a bird support payment intent.
/// </summary>
public sealed record BirdSupportPaymentRequest(
    Guid BirdId,
    int AmountCents,
    int WihngoAmountCents = 0
);

/// <summary>
/// Payment intent response.
/// Contains destination details for USDC payment.
/// </summary>
public sealed record CreatePaymentIntentResponse(
    Guid PaymentId,
    int AmountCents,
    string Currency,
    string DestinationWallet,
    string TokenMint,
    DateTime ExpiresAt,
    string ReturnUrl
);

/// <summary>
/// Request to confirm a payment.
/// </summary>
public sealed record ConfirmPaymentRequest(
    Guid PaymentId,
    string TxHash
);

/// <summary>
/// Payment confirmation response.
/// </summary>
public sealed record ConfirmPaymentResponse(
    Guid PaymentId,
    string Status,
    bool IsSuccess,
    string? FailureReason,
    DateTime? ConfirmedAt
);

/// <summary>
/// Payment status response.
/// </summary>
public sealed record PaymentStatusResponse(
    Guid PaymentId,
    string Status,
    string Purpose,
    Guid? BirdId,
    int AmountCents,
    string Currency,
    string Provider,
    DateTime CreatedAt,
    DateTime? ConfirmedAt
);

/// <summary>
/// Payment configuration for frontend.
/// </summary>
public sealed record PaymentConfigResponse(
    string Network,
    string RpcUrl,
    string UsdcMint,
    string PlatformWallet
);

/// <summary>
/// Simplified intent status for mobile polling.
/// </summary>
public sealed record IntentStatusResponse(
    string PaymentId,
    string Status,
    string? BirdId,
    string? BirdName,
    int AmountCents,
    string? Message,
    bool ClaimRequired = false,
    string? ClaimUrl = null
);

/// <summary>
/// Request to create a manual payment intent.
/// </summary>
public sealed record CreateManualPaymentRequest(
    Guid BirdId,
    int AmountCents,
    string Email,
    int WihngoAmountCents = 0
);

/// <summary>
/// Manual payment intent response.
/// </summary>
public sealed record CreateManualPaymentResponse(
    Guid PaymentId,
    int AmountCents,
    string Currency,
    string Network,
    string DestinationAddress,
    DateTime ExpiresAt,
    string ClaimUrl,
    string Message
);

/// <summary>
/// Response for claiming a payment.
/// </summary>
public sealed record ClaimPaymentResponse(
    Guid PaymentId,
    Guid? BirdId,
    string Message
);
