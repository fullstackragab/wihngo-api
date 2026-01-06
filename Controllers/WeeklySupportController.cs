using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for managing weekly support subscriptions (non-custodial recurring donations).
///
/// Flow:
/// 1. User subscribes to weekly support for a bird
/// 2. Each week: reminder notification sent
/// 3. User approves with 1-click (creates pre-filled SupportIntent)
/// 4. User signs in Phantom wallet
/// 5. Bird owner receives funds
/// </summary>
[ApiController]
[Route("api/weekly-support")]
[Authorize]
public class WeeklySupportController : ControllerBase
{
    private readonly IWeeklySupportService _weeklySupportService;
    private readonly ISupportIntentService _supportIntentService;
    private readonly ILogger<WeeklySupportController> _logger;

    public WeeklySupportController(
        IWeeklySupportService weeklySupportService,
        ISupportIntentService supportIntentService,
        ILogger<WeeklySupportController> logger)
    {
        _weeklySupportService = weeklySupportService;
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

    #region Subscription Management

    /// <summary>
    /// Get all weekly support subscriptions for current user
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(List<WeeklySupportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WeeklySupportSummaryDto>>> GetSubscriptions()
    {
        var userId = GetUserId();
        var subscriptions = await _weeklySupportService.GetUserSubscriptionsAsync(userId);
        return Ok(subscriptions);
    }

    /// <summary>
    /// Create a new weekly support subscription for a bird
    /// </summary>
    /// <remarks>
    /// Creates a subscription to support a bird weekly. Each week you'll receive a reminder
    /// to approve the payment. Default: $1.00 USDC per week, 100% goes to bird owner.
    /// </remarks>
    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(WeeklySupportSubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeeklySupportSubscriptionResponse>> CreateSubscription(
        [FromBody] CreateWeeklySupportRequest request)
    {
        var userId = GetUserId();

        _logger.LogInformation(
            "User {UserId} creating weekly subscription for bird {BirdId} at ${Amount}/week",
            userId, request.BirdId, request.AmountUsdc);

        var (response, error) = await _weeklySupportService.CreateSubscriptionAsync(userId, request);

        if (error != null)
        {
            return BadRequest(error);
        }

        return CreatedAtAction(
            nameof(GetSubscription),
            new { id = response!.SubscriptionId },
            response);
    }

    /// <summary>
    /// Get a specific subscription by ID
    /// </summary>
    [HttpGet("subscriptions/{id:guid}")]
    [ProducesResponseType(typeof(WeeklySupportSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySupportSubscriptionResponse>> GetSubscription(Guid id)
    {
        var userId = GetUserId();
        var subscription = await _weeklySupportService.GetSubscriptionAsync(id, userId);

        if (subscription == null)
        {
            return NotFound();
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Update subscription settings (amount, schedule)
    /// </summary>
    [HttpPut("subscriptions/{id:guid}")]
    [ProducesResponseType(typeof(WeeklySupportSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySupportSubscriptionResponse>> UpdateSubscription(
        Guid id,
        [FromBody] UpdateWeeklySupportRequest request)
    {
        var userId = GetUserId();
        var (response, error) = await _weeklySupportService.UpdateSubscriptionAsync(id, userId, request);

        if (error != null)
        {
            if (error.Message == "Subscription not found")
            {
                return NotFound();
            }
            return BadRequest(error);
        }

        return Ok(response);
    }

    /// <summary>
    /// Pause a subscription (stops sending reminders)
    /// </summary>
    [HttpPost("subscriptions/{id:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseSubscription(Guid id)
    {
        var userId = GetUserId();
        var success = await _weeklySupportService.PauseSubscriptionAsync(id, userId);

        if (!success)
        {
            return NotFound();
        }

        _logger.LogInformation("User {UserId} paused subscription {SubscriptionId}", userId, id);
        return NoContent();
    }

    /// <summary>
    /// Resume a paused subscription
    /// </summary>
    [HttpPost("subscriptions/{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeSubscription(Guid id)
    {
        var userId = GetUserId();
        var success = await _weeklySupportService.ResumeSubscriptionAsync(id, userId);

        if (!success)
        {
            return NotFound();
        }

        _logger.LogInformation("User {UserId} resumed subscription {SubscriptionId}", userId, id);
        return NoContent();
    }

    /// <summary>
    /// Cancel a subscription permanently
    /// </summary>
    [HttpDelete("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSubscription(Guid id)
    {
        var userId = GetUserId();
        var success = await _weeklySupportService.CancelSubscriptionAsync(id, userId);

        if (!success)
        {
            return NotFound();
        }

        _logger.LogInformation("User {UserId} cancelled subscription {SubscriptionId}", userId, id);
        return NoContent();
    }

    /// <summary>
    /// Get payment history for a subscription
    /// </summary>
    [HttpGet("subscriptions/{id:guid}/payments")]
    [ProducesResponseType(typeof(List<WeeklyPaymentHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WeeklyPaymentHistoryDto>>> GetPaymentHistory(
        Guid id,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        var history = await _weeklySupportService.GetPaymentHistoryAsync(id, userId, limit);
        return Ok(history);
    }

    #endregion

    #region Payment Approval

    /// <summary>
    /// Get pending payment approvals for current user
    /// </summary>
    /// <remarks>
    /// Returns weekly payments waiting for user approval.
    /// Each payment includes a deep link for 1-click approval.
    /// </remarks>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<WeeklyPaymentReminderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WeeklyPaymentReminderDto>>> GetPendingPayments()
    {
        var userId = GetUserId();
        var pending = await _weeklySupportService.GetPendingPaymentsAsync(userId);
        return Ok(pending);
    }

    /// <summary>
    /// Approve a weekly payment (creates pre-filled SupportIntent)
    /// </summary>
    /// <remarks>
    /// Creates a support intent for the pending weekly payment.
    /// Returns an unsigned Solana transaction for the user to sign in Phantom.
    ///
    /// After receiving the response:
    /// 1. Decode SerializedTransaction (base64)
    /// 2. Sign with Phantom wallet
    /// 3. Submit signed transaction via POST /api/support/intents/{intentId}/submit
    /// </remarks>
    [HttpPost("approve")]
    [ProducesResponseType(typeof(ApproveWeeklyPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApproveWeeklyPaymentResponse>> ApprovePayment(
        [FromBody] ApproveWeeklyPaymentRequest request)
    {
        var userId = GetUserId();

        _logger.LogInformation(
            "User {UserId} approving weekly payment {PaymentId}",
            userId, request.PaymentId);

        var (response, error) = await _weeklySupportService.CreateQuickPaymentIntentAsync(userId, request);

        if (error != null)
        {
            return BadRequest(error);
        }

        return Ok(response);
    }

    /// <summary>
    /// Submit signed transaction for weekly payment
    /// </summary>
    /// <remarks>
    /// After signing in Phantom, submit the signed transaction here.
    /// This delegates to the standard SupportIntent submit endpoint.
    /// </remarks>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitTransactionResponse>> SubmitSignedTransaction(
        [FromBody] SubmitWeeklyPaymentRequest request)
    {
        var userId = GetUserId();

        _logger.LogInformation(
            "User {UserId} submitting signed transaction for intent {IntentId}",
            userId, request.IntentId);

        var (response, error) = await _supportIntentService.SubmitSignedTransactionAsync(
            userId,
            request.IntentId,
            request.SignedTransaction,
            request.IdempotencyKey);

        if (error != null)
        {
            return BadRequest(error);
        }

        return Ok(response);
    }

    #endregion

    #region Summary & Stats

    /// <summary>
    /// Get user's weekly support summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(UserWeeklySupportSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserWeeklySupportSummaryDto>> GetUserSummary()
    {
        var userId = GetUserId();
        var summary = await _weeklySupportService.GetUserSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Get weekly supporters for a bird (owner only)
    /// </summary>
    [HttpGet("birds/{birdId:guid}/subscribers")]
    [ProducesResponseType(typeof(BirdWeeklySupportersDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BirdWeeklySupportersDto>> GetBirdSubscribers(Guid birdId)
    {
        var userId = GetUserId();
        var stats = await _weeklySupportService.GetBirdSubscribersAsync(birdId, userId);
        return Ok(stats);
    }

    #endregion
}

/// <summary>
/// Request to submit a signed weekly payment transaction
/// </summary>
public class SubmitWeeklyPaymentRequest
{
    /// <summary>
    /// The support intent ID from ApprovePayment response
    /// </summary>
    public Guid IntentId { get; set; }

    /// <summary>
    /// Base64-encoded signed Solana transaction
    /// </summary>
    public string SignedTransaction { get; set; } = string.Empty;

    /// <summary>
    /// Optional idempotency key
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
