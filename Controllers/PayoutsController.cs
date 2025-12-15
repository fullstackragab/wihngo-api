using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PayoutsController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly ILogger<PayoutsController> _logger;

        public PayoutsController(
            IPayoutService payoutService,
            ILogger<PayoutsController> logger)
        {
            _payoutService = payoutService;
            _logger = logger;
        }

        /// <summary>
        /// Get the current user's payout balance and earnings summary
        /// </summary>
        /// <returns>Payout balance information</returns>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                var userId = GetCurrentUserId();
                var balance = await _payoutService.GetBalanceAsync(userId);

                if (balance == null)
                {
                    return NotFound(new { message = "Balance not found" });
                }

                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout balance");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all payout methods for the current user
        /// </summary>
        /// <returns>List of payout methods</returns>
        [HttpGet("methods")]
        public async Task<IActionResult> GetMethods()
        {
            try
            {
                var userId = GetCurrentUserId();
                var methods = await _payoutService.GetPayoutMethodsAsync(userId);

                return Ok(new { methods });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout methods");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get a specific payout method by ID
        /// </summary>
        /// <param name="methodId">The payout method ID</param>
        /// <returns>Payout method details</returns>
        [HttpGet("methods/{methodId}")]
        public async Task<IActionResult> GetMethodById(Guid methodId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var method = await _payoutService.GetPayoutMethodByIdAsync(methodId, userId);

                if (method == null)
                {
                    return NotFound(new { message = "Payout method not found" });
                }

                return Ok(method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout method {MethodId}", methodId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Add a new payout method
        /// </summary>
        /// <param name="dto">Payout method creation data</param>
        /// <returns>Created payout method</returns>
        [HttpPost("methods")]
        public async Task<IActionResult> AddMethod([FromBody] PayoutMethodCreateDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var method = await _payoutService.AddPayoutMethodAsync(userId, dto);

                return CreatedAtAction(
                    nameof(GetMethodById),
                    new { methodId = method.Id },
                    new
                    {
                        id = method.Id,
                        userId = method.UserId,
                        methodType = method.MethodType,
                        isDefault = method.IsDefault,
                        isVerified = method.IsVerified,
                        createdAt = method.CreatedAt,
                        message = "Payout method added successfully"
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation error adding payout method: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payout method");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update a payout method (mainly to set as default)
        /// </summary>
        /// <param name="methodId">The payout method ID</param>
        /// <param name="dto">Update data</param>
        /// <returns>Updated payout method</returns>
        [HttpPatch("methods/{methodId}")]
        public async Task<IActionResult> UpdateMethod(Guid methodId, [FromBody] PayoutMethodUpdateDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var method = await _payoutService.UpdatePayoutMethodAsync(methodId, userId, dto);

                if (method == null)
                {
                    return NotFound(new { message = "Payout method not found" });
                }

                return Ok(new
                {
                    id = method.Id,
                    userId = method.UserId,
                    methodType = method.MethodType,
                    isDefault = method.IsDefault,
                    isVerified = method.IsVerified,
                    updatedAt = method.UpdatedAt,
                    message = "Payout method updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payout method {MethodId}", methodId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a payout method
        /// </summary>
        /// <param name="methodId">The payout method ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("methods/{methodId}")]
        public async Task<IActionResult> DeleteMethod(Guid methodId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _payoutService.DeletePayoutMethodAsync(methodId, userId);

                if (!deleted)
                {
                    return NotFound(new { message = "Payout method not found" });
                }

                return Ok(new { message = "Payout method deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot delete payout method {MethodId}: {Error}", methodId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payout method {MethodId}", methodId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get payout transaction history for the current user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <returns>Paginated payout history</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var history = await _payoutService.GetPayoutHistoryAsync(userId, page, pageSize, status);

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout history");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Process pending payouts (Admin only)
        /// </summary>
        /// <param name="dto">Optional user ID to process specific user's payout</param>
        /// <returns>Payout processing results</returns>
        [HttpPost("process")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessPayouts([FromBody] ProcessPayoutRequestDto? dto = null)
        {
            try
            {
                _logger.LogInformation("Processing payouts initiated by {AdminId}", GetCurrentUserId());
                
                var result = await _payoutService.ProcessPayoutsAsync(dto?.UserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payouts");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return Guid.Parse(userIdClaim);
        }
    }
}
