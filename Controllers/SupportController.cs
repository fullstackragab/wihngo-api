using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for USDC support transactions on Solana
/// </summary>
[ApiController]
[Route("api/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly IP2PPaymentService _transferService;
    private readonly ISupportIntentService _supportIntentService;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        IP2PPaymentService transferService,
        ISupportIntentService supportIntentService,
        ILogger<SupportController> logger)
    {
        _transferService = transferService;
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
    /// Validate a transfer before creating intent
    /// </summary>
    /// <remarks>
    /// Checks recipient exists, sender has sufficient balance, determines gas sponsorship need.
    /// Call this before creating an intent to show the user accurate fee information.
    /// </remarks>
    [HttpPost("transfers/preflight")]
    [ProducesResponseType(typeof(PreflightPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PreflightPaymentResponse>> PreflightTransfer([FromBody] PreflightPaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.PreflightPaymentAsync(userId, request);

            _logger.LogInformation(
                "Preflight transfer check: User {UserId} -> {RecipientId}, Amount: {Amount}, Valid: {Valid}",
                userId, request.RecipientId, request.Amount, result.Valid);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in preflight transfer");
            return BadRequest(new PreflightPaymentResponse
            {
                Valid = false,
                ErrorMessage = "An error occurred",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Create a P2P transfer intent (requires wallet, recipient user ID)
    /// </summary>
    /// <remarks>
    /// Creates a transfer record and builds an unsigned Solana transaction.
    /// The client should sign this transaction with Phantom and submit it.
    /// For bird support, use POST /api/support/intents instead.
    /// </remarks>
    [HttpPost("transfers")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentIntentResponse>> CreateTransferIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.CreatePaymentIntentAsync(userId, request);

            _logger.LogInformation(
                "Transfer intent created: {TransferId}, {Amount} USDC from {Sender} to {Recipient}",
                result.PaymentId, result.AmountUsdc, userId, request.RecipientUserId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Transfer intent creation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer intent");
            return StatusCode(500, new { error = "An error occurred creating the transfer" });
        }
    }

    /// <summary>
    /// Get a P2P transfer intent by ID
    /// </summary>
    [HttpGet("transfers/{transferId}")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusResponse>> GetTransferIntent(Guid transferId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.GetPaymentIntentAsync(transferId, userId);

            if (result == null)
            {
                return NotFound(new { error = "Transfer not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer intent {TransferId}", transferId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Submit a signed transaction for a transfer
    /// </summary>
    /// <remarks>
    /// After the user signs the transaction in Phantom, submit it here.
    /// The backend will submit to Solana and start tracking confirmations.
    /// </remarks>
    [HttpPost("transfers/submit")]
    [ProducesResponseType(typeof(SubmitTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitTransactionResponse>> SubmitTransferTransaction([FromBody] SubmitTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.SubmitSignedTransactionAsync(userId, request);

            if (result.Status == Models.Entities.PaymentStatus.Failed ||
                result.Status == Models.Entities.PaymentStatus.Expired)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Transfer {TransferId} submitted: Signature {Signature}",
                result.PaymentId, result.SolanaSignature);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting transaction for transfer {TransferId}", request.PaymentId);
            return StatusCode(500, new SubmitTransactionResponse
            {
                PaymentId = request.PaymentId,
                Status = Models.Entities.PaymentStatus.Failed,
                ErrorMessage = "An error occurred submitting the transaction"
            });
        }
    }

    /// <summary>
    /// Cancel a pending transfer
    /// </summary>
    [HttpPost("transfers/{transferId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTransfer(Guid transferId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.CancelPaymentAsync(transferId, userId);

            if (!result)
            {
                return BadRequest(new { error = "Transfer cannot be cancelled. It may already be submitted or completed." });
            }

            _logger.LogInformation("Transfer {TransferId} cancelled by user {UserId}", transferId, userId);
            return Ok(new { success = true, message = "Transfer cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer {TransferId}", transferId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Get user's transfer history
    /// </summary>
    [HttpGet("transfers")]
    [ProducesResponseType(typeof(PagedResult<PaymentSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentSummary>>> GetTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transferService.GetUserPaymentsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfers");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    // =============================================
    // BIRD SUPPORT ENDPOINTS (USDC on Solana)
    // MVP: Non-custodial - users support directly from their wallets
    // =============================================

    /// <summary>
    /// Check if user can support a bird (preflight)
    /// </summary>
    /// <remarks>
    /// Call this before showing the support UI. Returns:
    /// - Whether user has a connected wallet
    /// - User's USDC and SOL balances
    /// - Whether balances are sufficient
    /// - Whether gas will be sponsored
    /// - Recipient and bird info
    ///
    /// If hasWallet is false, prompt user to connect wallet first.
    /// </remarks>
    [HttpPost("birds/preflight")]
    [ProducesResponseType(typeof(CheckSupportBalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckSupportBalanceResponse>> PreflightBirdSupport([FromBody] CheckSupportBalanceRequest request)
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
    /// 1. Call POST /birds/preflight to check balances
    /// 2. If canSupport=true, call POST /intents
    /// 3. Sign the returned serializedTransaction in wallet
    /// 4. Call POST /intents/{id}/submit with signed transaction
    ///
    /// Request body:
    /// - birdId: The bird to support (required)
    /// - supportAmount: Amount in USDC to give to bird owner (required)
    /// - platformSupportAmount: Optional contribution to platform in USDC (default 0)
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
    /// Support Wihngo directly (no bird involved)
    /// </summary>
    /// <remarks>
    /// Use this endpoint when supporting Wihngo platform directly, without a specific bird.
    /// Minimum support amount: $0.05 USDC.
    ///
    /// Flow:
    /// 1. Call POST /wihngo with amount
    /// 2. Sign the returned serializedTransaction in wallet
    /// 3. Call POST /intents/{id}/submit with signed transaction
    /// </remarks>
    [HttpPost("wihngo")]
    [ProducesResponseType(typeof(SupportWihngoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SupportWihngo([FromBody] SupportWihngoRequest request)
    {
        try
        {
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
            var (response, error) = await _supportIntentService.CreateWihngoSupportIntentAsync(userId, request);

            if (error != null)
            {
                _logger.LogWarning(
                    "Wihngo support intent creation failed: {ErrorCode} - {Message}",
                    error.ErrorCode, error.Message);
                return BadRequest(error);
            }

            _logger.LogInformation(
                "Wihngo support intent created: {IntentId}, Amount: {Amount} USDC",
                response!.IntentId, response.Amount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Wihngo support intent");
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
    ///
    /// Idempotency: If the same idempotencyKey is used for a request that was already
    /// processed, the original result will be returned (with wasAlreadySubmitted=true).
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
                userId, intentId, request.SignedTransaction, request.IdempotencyKey);

            if (error != null)
            {
                _logger.LogWarning(
                    "Transaction submission failed for intent {IntentId}: {ErrorCode}",
                    intentId, error.ErrorCode);
                return BadRequest(error);
            }

            _logger.LogInformation(
                "Transaction submitted for intent {IntentId}: {Signature}, WasAlreadySubmitted: {WasAlready}",
                intentId, response!.SolanaSignature, response.WasAlreadySubmitted);

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
    [HttpGet("history")]
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
