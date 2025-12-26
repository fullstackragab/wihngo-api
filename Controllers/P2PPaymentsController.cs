using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for P2P USDC payments on Solana
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class P2PPaymentsController : ControllerBase
{
    private readonly IP2PPaymentService _paymentService;
    private readonly ISupportIntentService _supportIntentService;
    private readonly ILogger<P2PPaymentsController> _logger;

    public P2PPaymentsController(
        IP2PPaymentService paymentService,
        ISupportIntentService supportIntentService,
        ILogger<P2PPaymentsController> logger)
    {
        _paymentService = paymentService;
        _supportIntentService = supportIntentService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Validate a payment before creating intent
    /// </summary>
    /// <remarks>
    /// Checks recipient exists, sender has sufficient balance, determines gas sponsorship need.
    /// Call this before creating an intent to show the user accurate fee information.
    /// </remarks>
    [HttpPost("preflight")]
    [ProducesResponseType(typeof(PreflightPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PreflightPaymentResponse>> PreflightPayment([FromBody] PreflightPaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.PreflightPaymentAsync(userId, request);

            _logger.LogInformation(
                "Preflight payment check: User {UserId} -> {RecipientId}, Amount: {Amount}, Valid: {Valid}",
                userId, request.RecipientId, request.Amount, result.Valid);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in preflight payment");
            return BadRequest(new PreflightPaymentResponse
            {
                Valid = false,
                ErrorMessage = "An error occurred",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Create a P2P payment intent (requires wallet, recipient user ID)
    /// </summary>
    /// <remarks>
    /// Creates a payment record and builds an unsigned Solana transaction.
    /// The client should sign this transaction with Phantom and submit it.
    /// For bird support/donations, use POST /api/payments/intents instead.
    /// </remarks>
    [HttpPost("p2p-intents")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.CreatePaymentIntentAsync(userId, request);

            _logger.LogInformation(
                "Payment intent created: {PaymentId}, {Amount} USDC from {Sender} to {Recipient}",
                result.PaymentId, result.AmountUsdc, userId, request.RecipientUserId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Payment intent creation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new { error = "An error occurred creating the payment" });
        }
    }

    /// <summary>
    /// Get a P2P payment intent by ID
    /// </summary>
    [HttpGet("p2p-intents/{paymentId}")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentIntent(Guid paymentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.GetPaymentIntentAsync(paymentId, userId);

            if (result == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment intent {PaymentId}", paymentId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Submit a signed transaction
    /// </summary>
    /// <remarks>
    /// After the user signs the transaction in Phantom, submit it here.
    /// The backend will submit to Solana and start tracking confirmations.
    /// </remarks>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitTransactionResponse>> SubmitTransaction([FromBody] SubmitTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.SubmitSignedTransactionAsync(userId, request);

            if (result.Status == Models.Entities.PaymentStatus.Failed ||
                result.Status == Models.Entities.PaymentStatus.Expired)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Payment {PaymentId} submitted: Signature {Signature}",
                result.PaymentId, result.SolanaSignature);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting transaction for payment {PaymentId}", request.PaymentId);
            return StatusCode(500, new SubmitTransactionResponse
            {
                PaymentId = request.PaymentId,
                Status = Models.Entities.PaymentStatus.Failed,
                ErrorMessage = "An error occurred submitting the transaction"
            });
        }
    }

    /// <summary>
    /// Cancel a pending payment
    /// </summary>
    [HttpPost("{paymentId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayment(Guid paymentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.CancelPaymentAsync(paymentId, userId);

            if (!result)
            {
                return BadRequest(new { error = "Payment cannot be cancelled. It may already be submitted or completed." });
            }

            _logger.LogInformation("Payment {PaymentId} cancelled by user {UserId}", paymentId, userId);
            return Ok(new { success = true, message = "Payment cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Get user's payment history
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentSummary>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.GetUserPaymentsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    // =============================================
    // SUPPORT INTENT ENDPOINTS (Bird Donations - USDC on Solana)
    // MVP: Non-custodial - users pay directly from their wallets
    // =============================================

    /// <summary>
    /// Check if user can support a bird (preflight)
    /// </summary>
    /// <remarks>
    /// Call this before showing the payment UI. Returns:
    /// - Whether user has a connected wallet
    /// - User's USDC and SOL balances
    /// - Whether balances are sufficient
    /// - Whether gas will be sponsored
    /// - Recipient and bird info
    ///
    /// If hasWallet is false, prompt user to connect wallet first.
    /// </remarks>
    [HttpPost("support/preflight")]
    [ProducesResponseType(typeof(CheckSupportBalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckSupportBalanceResponse>> PreflightSupport([FromBody] CheckSupportBalanceRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _supportIntentService.CheckSupportBalanceAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking support balance for bird {BirdId}", request.BirdId);
            return Ok(new CheckSupportBalanceResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred checking balance"
            });
        }
    }

    /// <summary>
    /// Create a support intent for a bird
    /// </summary>
    /// <remarks>
    /// MVP: Requires connected wallet with sufficient USDC + SOL.
    /// Returns an unsigned Solana transaction for the user to sign.
    ///
    /// Flow:
    /// 1. Call POST /preflight to check balances
    /// 2. If canSupport=true, call POST /intents
    /// 3. Sign the returned serializedTransaction in wallet
    /// 4. Call POST /intents/{id}/submit with signed transaction
    ///
    /// Request body:
    /// - birdId: The bird to support (required)
    /// - supportAmount: Amount in USDC to give to bird owner (required)
    /// - platformSupportAmount: Optional tip to platform in USDC (default 0)
    /// - currency: USDC only for MVP
    /// </remarks>
    [HttpPost("intents")]
    [ProducesResponseType(typeof(SupportIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntent([FromBody] CreateSupportIntentRequest request)
    {
        try
        {
            // Handle model validation errors
            if (!ModelState.IsValid)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var kvp in ModelState)
                {
                    if (kvp.Value.Errors.Count > 0)
                    {
                        errors[ToCamelCase(kvp.Key)] = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                    }
                }

                return BadRequest(new ValidationErrorResponse
                {
                    ErrorCode = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    FieldErrors = errors
                });
            }

            var userId = GetUserId();
            var (response, error) = await _supportIntentService.CreateSupportIntentAsync(userId, request);

            if (error != null)
            {
                _logger.LogWarning(
                    "Support intent creation failed: {ErrorCode} - {Message}",
                    error.ErrorCode, error.Message);
                return BadRequest(error);
            }

            _logger.LogInformation(
                "Support intent created: {IntentId} for bird {BirdId}, Amount: {Amount}",
                response!.IntentId, request.BirdId, response.TotalAmount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating support intent for bird {BirdId}", request.BirdId);
            return StatusCode(500, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred creating the support intent"
            });
        }
    }

    /// <summary>
    /// Get a support intent by ID
    /// </summary>
    [HttpGet("intents/{intentId}")]
    [ProducesResponseType(typeof(SupportIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntent(Guid intentId)
    {
        try
        {
            var userId = GetUserId();
            var response = await _supportIntentService.GetSupportIntentResponseAsync(intentId, userId);

            if (response == null)
            {
                return NotFound(new ValidationErrorResponse
                {
                    ErrorCode = SupportIntentErrorCodes.IntentNotFound,
                    Message = "Support intent not found"
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting support intent {IntentId}", intentId);
            return StatusCode(500, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Submit a signed transaction for a support intent
    /// </summary>
    /// <remarks>
    /// After creating an intent and signing the transaction in the user's wallet,
    /// call this endpoint to submit the signed transaction to Solana.
    ///
    /// The transaction will be submitted to the network and the intent status
    /// will change to "processing". A background job monitors confirmation.
    /// </remarks>
    [HttpPost("intents/{intentId}/submit")]
    [ProducesResponseType(typeof(SubmitTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitSupportTransaction(Guid intentId, [FromBody] SubmitTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var (response, error) = await _supportIntentService.SubmitSignedTransactionAsync(
                userId, intentId, request.SignedTransaction);

            if (error != null)
            {
                _logger.LogWarning(
                    "Transaction submission failed for intent {IntentId}: {ErrorCode}",
                    intentId, error.ErrorCode);
                return BadRequest(error);
            }

            _logger.LogInformation(
                "Transaction submitted for intent {IntentId}: {Signature}",
                intentId, response!.SolanaSignature);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting transaction for intent {IntentId}", intentId);
            return StatusCode(500, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred submitting the transaction"
            });
        }
    }

    /// <summary>
    /// Cancel a pending support intent
    /// </summary>
    [HttpPost("intents/{intentId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelIntent(Guid intentId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _supportIntentService.CancelSupportIntentAsync(intentId, userId);

            if (!success)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    ErrorCode = SupportIntentErrorCodes.InternalError,
                    Message = "Support intent cannot be cancelled. It may already be completed or cancelled."
                });
            }

            return Ok(new { success = true, message = "Support intent cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling support intent {IntentId}", intentId);
            return StatusCode(500, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Get user's support history
    /// </summary>
    [HttpGet("support-history")]
    [ProducesResponseType(typeof(PagedResult<SupportIntentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupportIntentResponse>>> GetSupportHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            var result = await _supportIntentService.GetUserSupportHistoryAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting support history");
            return StatusCode(500, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "An error occurred"
            });
        }
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
