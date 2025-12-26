using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

/// <summary>
/// Controller for Kind Words - a constrained, care-focused comment system.
/// Kind words are supportive messages left on bird stories.
/// </summary>
[ApiController]
[Route("api/birds/{birdId}/kind-words")]
public class KindWordsController : ControllerBase
{
    private readonly IKindWordsService _kindWordsService;
    private readonly ILogger<KindWordsController> _logger;

    public KindWordsController(
        IKindWordsService kindWordsService,
        ILogger<KindWordsController> logger)
    {
        _kindWordsService = kindWordsService;
        _logger = logger;
    }

    private Guid? GetUserIdOrNull()
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
        var userId = GetUserIdOrNull();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }
        return userId.Value;
    }

    /// <summary>
    /// Get kind words for a bird.
    /// Returns the kind words section including eligibility to post.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(KindWordsSectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KindWordsSectionDto>> GetKindWords(
        Guid birdId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserIdOrNull();
            var result = await _kindWordsService.GetKindWordsSectionAsync(birdId, userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting kind words for bird {BirdId}", birdId);
            return StatusCode(500, new { message = "Unable to load kind words" });
        }
    }

    /// <summary>
    /// Post a kind word on a bird.
    /// User must have supported or follow the bird.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(KindWordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KindWordResultDto>> PostKindWord(
        Guid birdId,
        [FromBody] KindWordCreateDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _kindWordsService.PostKindWordAsync(birdId, userId, dto);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Please sign in to leave a kind word" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting kind word on bird {BirdId}", birdId);
            return StatusCode(500, new { message = "Unable to post your kind words" });
        }
    }

    /// <summary>
    /// Delete a kind word.
    /// Bird owner can delete any kind word; authors can delete their own.
    /// </summary>
    [HttpDelete("{kindWordId}")]
    [Authorize]
    [ProducesResponseType(typeof(KindWordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KindWordResultDto>> DeleteKindWord(Guid birdId, Guid kindWordId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _kindWordsService.DeleteKindWordAsync(kindWordId, userId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting kind word {KindWordId}", kindWordId);
            return StatusCode(500, new { message = "Unable to remove kind word" });
        }
    }

    /// <summary>
    /// Toggle kind words on/off for a bird.
    /// Only the bird owner can change this setting.
    /// </summary>
    [HttpPut("settings")]
    [Authorize]
    [ProducesResponseType(typeof(KindWordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KindWordResultDto>> UpdateSettings(
        Guid birdId,
        [FromBody] KindWordsSettingsDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _kindWordsService.SetKindWordsEnabledAsync(birdId, userId, dto.KindWordsEnabled);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating kind words settings for bird {BirdId}", birdId);
            return StatusCode(500, new { message = "Unable to update settings" });
        }
    }

    /// <summary>
    /// Block a user from posting kind words on a specific bird.
    /// Silent block - no notification to the blocked user.
    /// </summary>
    [HttpPost("block/{userToBlockId}")]
    [Authorize]
    [ProducesResponseType(typeof(KindWordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KindWordResultDto>> BlockUser(Guid birdId, Guid userToBlockId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _kindWordsService.BlockUserAsync(birdId, userId, userToBlockId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user {UserToBlockId} from bird {BirdId}", userToBlockId, birdId);
            return StatusCode(500, new { message = "Unable to block user" });
        }
    }

    /// <summary>
    /// Unblock a user from posting kind words on a specific bird.
    /// </summary>
    [HttpDelete("block/{userToUnblockId}")]
    [Authorize]
    [ProducesResponseType(typeof(KindWordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KindWordResultDto>> UnblockUser(Guid birdId, Guid userToUnblockId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _kindWordsService.UnblockUserAsync(birdId, userId, userToUnblockId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {UserToUnblockId} from bird {BirdId}", userToUnblockId, birdId);
            return StatusCode(500, new { message = "Unable to unblock user" });
        }
    }
}
