using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for wallet connection intents.
///
/// Solves the Android browser-switch problem:
/// When Phantom redirects back after signing, Android may open a different browser,
/// losing the user's JWT session. This controller provides:
///
/// 1. Intent creation (authenticated) - creates state token and nonce
/// 2. Public callback - validates signature without auth, returns new tokens
/// 3. Status checking - for polling and recovery flows
///
/// Flow:
/// 1. POST /api/wallet-connect/intents → Get state + nonce (requires auth)
/// 2. Redirect to Phantom with state parameter
/// 3. POST /api/wallet-connect/callback → Validate & link (PUBLIC - no auth)
/// 4. Frontend receives new tokens for session recovery
/// </summary>
[ApiController]
[Route("api/wallet-connect")]
public class WalletConnectController : ControllerBase
{
    private readonly IWalletConnectIntentService _intentService;
    private readonly ILogger<WalletConnectController> _logger;

    public WalletConnectController(
        IWalletConnectIntentService intentService,
        ILogger<WalletConnectController> logger)
    {
        _intentService = intentService;
        _logger = logger;
    }

    private Guid? TryGetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    private Guid GetUserId()
    {
        var userId = TryGetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId.Value;
    }

    /// <summary>
    /// Create a new wallet connection intent
    /// </summary>
    /// <remarks>
    /// Call this before redirecting to Phantom. Returns a state token and nonce
    /// that will be used to validate the callback.
    ///
    /// The state token should be passed to Phantom in the redirect URL.
    /// Phantom will return it in the callback, allowing us to match the response
    /// to the original request even if the callback arrives in a different browser.
    ///
    /// Flow:
    /// 1. Call this endpoint → get state, nonce, callbackUrl
    /// 2. Redirect user to Phantom with state parameter
    /// 3. Phantom signs the nonce message
    /// 4. Phantom redirects to callbackUrl with state, publicKey, signature
    /// 5. POST callback endpoint to complete the connection
    /// </remarks>
    [HttpPost("intents")]
    [Authorize]
    [ProducesResponseType(typeof(WalletConnectIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WalletConnectIntentResponse>> CreateIntent([FromBody] CreateWalletConnectIntentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _intentService.CreateIntentAsync(userId, request, ipAddress, userAgent);

            _logger.LogInformation(
                "Wallet connect intent created: {IntentId} for user {UserId}",
                response.IntentId, userId);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required", code = "AUTH_REQUIRED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet connect intent");
            return StatusCode(500, new
            {
                success = false,
                errorCode = WalletConnectErrorCodes.InternalError,
                message = "Failed to create connection intent"
            });
        }
    }

    /// <summary>
    /// Process wallet connection callback (PUBLIC - no auth required)
    /// </summary>
    /// <remarks>
    /// This is the PUBLIC callback endpoint that Phantom redirects to.
    /// It does NOT require authentication because:
    ///
    /// 1. Android may open this in a different browser without the user's session
    /// 2. The state token provides CSRF protection
    /// 3. The signature proves wallet ownership
    ///
    /// On success, returns:
    /// - walletId: The linked wallet ID
    /// - publicKey: The wallet public key
    /// - accessToken: New JWT for session recovery (if user was authenticated)
    /// - refreshToken: New refresh token (if user was authenticated)
    /// - redirectUrl: Where to navigate next
    /// - metadata: Any data passed during intent creation
    ///
    /// The frontend should store the new tokens and redirect to the continuation page.
    /// </remarks>
    [HttpPost("callback")]
    [AllowAnonymous] // CRITICAL: This must be public for Android browser-switch
    [ProducesResponseType(typeof(WalletConnectCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WalletConnectCallbackResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletConnectCallbackResponse>> ProcessCallback([FromBody] WalletConnectCallbackRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Processing wallet connect callback: State={State}, PublicKey={PublicKey}",
                request.State, request.PublicKey);

            var response = await _intentService.ProcessCallbackAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Wallet connect callback failed: {ErrorCode} - {Message}",
                    response.ErrorCode, response.Message);

                return BadRequest(response);
            }

            _logger.LogInformation(
                "Wallet connect callback success: WalletId={WalletId}, UserId={UserId}, IsNew={IsNew}",
                response.WalletId, response.UserId, response.IsNewWallet);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wallet connect callback");
            return StatusCode(500, new WalletConnectCallbackResponse
            {
                Success = false,
                ErrorCode = WalletConnectErrorCodes.InternalError,
                Message = "Failed to process connection callback"
            });
        }
    }

    /// <summary>
    /// Get callback URL info (for frontend configuration)
    /// </summary>
    /// <remarks>
    /// Returns the callback URL that should be configured in Phantom.
    /// This is useful for frontend to know where Phantom should redirect.
    /// </remarks>
    [HttpGet("callback-info")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetCallbackInfo()
    {
        var baseUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
        return Ok(new
        {
            callbackUrl = $"{baseUrl}/api/wallet-connect/callback",
            method = "POST",
            description = "POST the state, publicKey, and signature to this URL after Phantom signing"
        });
    }

    /// <summary>
    /// Get intent status
    /// </summary>
    /// <remarks>
    /// Check the status of a wallet connection intent.
    /// Can be used for polling or recovery flows.
    ///
    /// The intentIdOrState parameter can be either:
    /// - The intent ID (GUID)
    /// - The state token
    /// </remarks>
    [HttpGet("intents/{intentIdOrState}")]
    [Authorize]
    [ProducesResponseType(typeof(WalletConnectIntentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletConnectIntentStatusResponse>> GetIntentStatus(string intentIdOrState)
    {
        try
        {
            var userId = GetUserId();
            var status = await _intentService.GetIntentStatusAsync(intentIdOrState, userId);

            if (status == null)
            {
                return NotFound(new
                {
                    errorCode = WalletConnectErrorCodes.IntentNotFound,
                    message = "Intent not found"
                });
            }

            return Ok(status);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting intent status for {IntentId}", intentIdOrState);
            return StatusCode(500, new
            {
                errorCode = WalletConnectErrorCodes.InternalError,
                message = "Failed to get intent status"
            });
        }
    }

    /// <summary>
    /// Check for pending wallet connection intent
    /// </summary>
    /// <remarks>
    /// Returns any pending (not completed/expired) wallet connection intent for the user.
    /// Useful for recovery flows where the user may have left the app mid-connection.
    ///
    /// If there's a pending intent, the frontend can:
    /// 1. Show a "continue connection" prompt
    /// 2. Cancel and start fresh
    /// </remarks>
    [HttpGet("pending")]
    [Authorize]
    [ProducesResponseType(typeof(PendingWalletIntentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PendingWalletIntentResponse>> GetPendingIntent()
    {
        try
        {
            var userId = GetUserId();
            var response = await _intentService.GetPendingIntentAsync(userId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pending intent");
            return StatusCode(500, new PendingWalletIntentResponse
            {
                HasPendingIntent = false,
                Message = "Failed to check pending intents"
            });
        }
    }

    /// <summary>
    /// Cancel a pending wallet connection intent
    /// </summary>
    [HttpPost("intents/{intentIdOrState}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelIntent(string intentIdOrState)
    {
        try
        {
            var userId = GetUserId();
            var success = await _intentService.CancelIntentAsync(intentIdOrState, userId);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    errorCode = WalletConnectErrorCodes.IntentNotFound,
                    message = "Intent not found or already completed"
                });
            }

            return Ok(new { success = true, message = "Intent cancelled" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling intent {IntentId}", intentIdOrState);
            return StatusCode(500, new
            {
                success = false,
                errorCode = WalletConnectErrorCodes.InternalError,
                message = "Failed to cancel intent"
            });
        }
    }

    /// <summary>
    /// Get client IP address from request
    /// </summary>
    private string? GetClientIpAddress()
    {
        // Check for forwarded headers (behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP (client IP)
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
