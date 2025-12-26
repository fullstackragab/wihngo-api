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
    private readonly ILogger<P2PPaymentsController> _logger;

    public P2PPaymentsController(
        IP2PPaymentService paymentService,
        ILogger<P2PPaymentsController> logger)
    {
        _paymentService = paymentService;
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
    /// Create a payment intent
    /// </summary>
    /// <remarks>
    /// Creates a payment record and builds an unsigned Solana transaction.
    /// The client should sign this transaction with Phantom and submit it.
    /// </remarks>
    [HttpPost("intents")]
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
    /// Get a payment intent by ID
    /// </summary>
    [HttpGet("intents/{paymentId}")]
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
}
