using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for managing user wallet linking (Phantom)
/// </summary>
[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(
        IWalletService walletService,
        ILedgerService ledgerService,
        ILogger<WalletsController> logger)
    {
        _walletService = walletService;
        _ledgerService = ledgerService;
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
    /// Link a Phantom wallet to the user's account
    /// </summary>
    [HttpPost("link")]
    [ProducesResponseType(typeof(LinkWalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LinkWalletResponse>> LinkWallet([FromBody] LinkWalletRequest request)
    {
        try
        {
            var userId = GetUserId();

            // Validate public key format (base58, ~44 chars)
            if (string.IsNullOrEmpty(request.PublicKey) || request.PublicKey.Length < 32 || request.PublicKey.Length > 44)
            {
                return BadRequest(new { error = "Invalid wallet public key" });
            }

            var result = await _walletService.LinkWalletAsync(userId, request);

            _logger.LogInformation(
                "Wallet linked: User {UserId} -> {PublicKey}",
                userId, request.PublicKey);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Wallet linking failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking wallet");
            return StatusCode(500, new { error = "An error occurred linking the wallet" });
        }
    }

    /// <summary>
    /// Get all wallets linked to the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserWalletsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserWalletsResponse>> GetWallets()
    {
        try
        {
            var userId = GetUserId();
            var result = await _walletService.GetUserWalletsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallets");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Unlink a wallet from the user's account
    /// </summary>
    [HttpDelete("{walletId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkWallet(Guid walletId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _walletService.UnlinkWalletAsync(userId, walletId);

            if (!result)
            {
                return NotFound(new { error = "Wallet not found" });
            }

            _logger.LogInformation("Wallet {WalletId} unlinked from user {UserId}", walletId, userId);
            return Ok(new { success = true, message = "Wallet unlinked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking wallet {WalletId}", walletId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Set a wallet as the primary wallet
    /// </summary>
    [HttpPost("{walletId}/primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryWallet(Guid walletId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _walletService.SetPrimaryWalletAsync(userId, walletId);

            if (!result)
            {
                return NotFound(new { error = "Wallet not found" });
            }

            return Ok(new { success = true, message = "Primary wallet updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary wallet {WalletId}", walletId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Get current user's USDC balance
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(UserBalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserBalanceResponse>> GetBalance()
    {
        try
        {
            var userId = GetUserId();
            var result = await _ledgerService.GetFullBalanceAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }
}
