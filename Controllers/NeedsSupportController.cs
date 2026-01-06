using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for the "birds need support" feature.
/// Each bird can be supported once per week.
/// </summary>
[ApiController]
[Route("api/needs-support")]
public class NeedsSupportController : ControllerBase
{
    private readonly INeedsSupportService _needsSupportService;
    private readonly ILogger<NeedsSupportController> _logger;

    public NeedsSupportController(
        INeedsSupportService needsSupportService,
        ILogger<NeedsSupportController> logger)
    {
        _needsSupportService = needsSupportService;
        _logger = logger;
    }

    /// <summary>
    /// Get all birds needing support.
    /// This is the main endpoint for the "birds need support" page.
    /// </summary>
    /// <remarks>
    /// Returns birds that need support this week.
    ///
    /// How it works:
    /// - All birds marked as 'needs support' are shown
    /// - Each bird can receive support once per week
    /// - When all birds have been supported: Thank you message, no birds shown
    /// - Resets every Sunday
    /// </remarks>
    [HttpGet("birds")]
    [ProducesResponseType(typeof(BirdsNeedSupportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BirdsNeedSupportResponse>> GetBirdsNeedingSupport()
    {
        var response = await _needsSupportService.GetBirdsNeedingSupportAsync();
        return Ok(response);
    }

    /// <summary>
    /// Get weekly support statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(WeeklySupportStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklySupportStatsDto>> GetWeeklyStats()
    {
        var stats = await _needsSupportService.GetWeeklyStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Get weekly support progress for a specific bird.
    /// </summary>
    [HttpGet("birds/{birdId}/progress")]
    [ProducesResponseType(typeof(BirdWeeklySupportProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BirdWeeklySupportProgressDto>> GetBirdProgress(Guid birdId)
    {
        var progress = await _needsSupportService.GetBirdWeeklyProgressAsync(birdId);

        if (progress == null)
        {
            return NotFound(new { message = "Bird not found" });
        }

        return Ok(progress);
    }

    /// <summary>
    /// Set whether a bird needs support (owner only).
    /// This controls whether the bird appears in the "needs support" list.
    /// </summary>
    [HttpPatch("birds/{birdId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBirdNeedsSupport(Guid birdId, [FromBody] SetNeedsSupportRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var success = await _needsSupportService.SetBirdNeedsSupportAsync(birdId, userId, request.NeedsSupport);

        if (!success)
        {
            return NotFound(new { message = "Bird not found or you don't own this bird" });
        }

        return Ok(new
        {
            message = request.NeedsSupport
                ? "Your bird is now visible in the 'birds need support' list"
                : "Your bird has been removed from the 'birds need support' list",
            birdId,
            needsSupport = request.NeedsSupport
        });
    }

    /// <summary>
    /// Check if a bird can receive support in the current round.
    /// Useful for UI to show/hide support buttons.
    /// </summary>
    [HttpGet("birds/{birdId}/can-support")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanSupportBird(Guid birdId)
    {
        var canSupport = await _needsSupportService.CanBirdReceiveSupportAsync(birdId);

        return Ok(new
        {
            birdId,
            canReceiveSupport = canSupport,
            message = canSupport
                ? "This bird can receive support"
                : "This bird has already been supported this week"
        });
    }
}
