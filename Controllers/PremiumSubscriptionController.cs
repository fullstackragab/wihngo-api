namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using Wihngo.Dtos;
    using Wihngo.Services.Interfaces;

    [ApiController]
    [Route("api/premium")]
    [Authorize]
    public class PremiumSubscriptionController : ControllerBase
    {
        private readonly IPremiumSubscriptionService _subscriptionService;
        private readonly ILogger<PremiumSubscriptionController> _logger;

        public PremiumSubscriptionController(
            IPremiumSubscriptionService subscriptionService,
            ILogger<PremiumSubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Subscribe a bird to premium
        /// POST /api/premium/subscribe
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<ActionResult<SubscriptionResponseDto>> Subscribe([FromBody] SubscribeDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid user authentication" });

                var result = await _subscriptionService.CreateSubscriptionAsync(request, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating premium subscription");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get premium status for a bird
        /// GET /api/premium/status/{birdId}
        /// </summary>
        [HttpGet("status/{birdId}")]
        [AllowAnonymous]
        public async Task<ActionResult<PremiumStatusResponseDto>> GetStatus(Guid birdId)
        {
            try
            {
                var status = await _subscriptionService.GetPremiumStatusAsync(birdId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting premium status for bird {BirdId}", birdId);
                return StatusCode(500, new { message = "An error occurred while retrieving premium status" });
            }
        }

        /// <summary>
        /// Cancel a premium subscription
        /// POST /api/premium/cancel/{birdId}
        /// </summary>
        [HttpPost("cancel/{birdId}")]
        public async Task<ActionResult> CancelSubscription(Guid birdId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid user authentication" });

                await _subscriptionService.CancelSubscriptionAsync(birdId, userId);
                return Ok(new { message = "Subscription canceled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling subscription for bird {BirdId}", birdId);
                return StatusCode(500, new { message = "An error occurred while canceling subscription" });
            }
        }

        /// <summary>
        /// Update premium style for a bird
        /// PUT /api/premium/style/{birdId}
        /// </summary>
        [HttpPut("style/{birdId}")]
        public async Task<ActionResult<PremiumStyleDto>> UpdateStyle(Guid birdId, [FromBody] UpdatePremiumStyleDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid user authentication" });

                var style = await _subscriptionService.UpdatePremiumStyleAsync(birdId, request, userId);
                return Ok(style);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating premium style for bird {BirdId}", birdId);
                return StatusCode(500, new { message = "An error occurred while updating premium style" });
            }
        }

        /// <summary>
        /// Get premium style for a bird
        /// GET /api/premium/style/{birdId}
        /// </summary>
        [HttpGet("style/{birdId}")]
        [AllowAnonymous]
        public async Task<ActionResult<PremiumStyleDto>> GetStyle(Guid birdId)
        {
            try
            {
                var style = await _subscriptionService.GetPremiumStyleAsync(birdId);
                if (style == null)
                    return NotFound(new { message = "Premium style not found" });

                return Ok(style);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting premium style for bird {BirdId}", birdId);
                return StatusCode(500, new { message = "An error occurred while retrieving premium style" });
            }
        }

        /// <summary>
        /// Get available premium plans
        /// GET /api/premium/plans
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<PremiumPlanDto>> GetPlans()
        {
            var plans = new[]
            {
                new PremiumPlanDto
                {
                    Id = "monthly",
                    Name = "Monthly Celebration",
                    Price = 3.99m,
                    Currency = "USD",
                    Interval = "month",
                    Description = "Show your love & support your bird monthly",
                    CharityAllocation = 10,
                    Features = new[]
                    {
                        "Custom profile theme & cover",
                        "Highlighted Best Moments",
                        "'Celebrated Bird' badge",
                        "Unlimited photos & videos",
                        "Memory collages & story albums",
                        "QR code for profile sharing",
                        "Pin up to 5 story highlights",
                        "Donation tracker display"
                    }
                },
                new PremiumPlanDto
                {
                    Id = "yearly",
                    Name = "Yearly Celebration",
                    Price = 39.99m,
                    Currency = "USD",
                    Interval = "year",
                    Savings = "Save $8/year - 2 months free!",
                    Description = "A year of love & celebration for your bird",
                    CharityAllocation = 15,
                    Features = new[]
                    {
                        "All Monthly features included",
                        "Custom profile theme & cover",
                        "Highlighted Best Moments",
                        "'Celebrated Bird' badge",
                        "Unlimited photos & videos",
                        "Memory collages & story albums",
                        "QR code for profile sharing",
                        "Pin up to 5 story highlights",
                        "Donation tracker display",
                        "Priority support"
                    }
                },
                new PremiumPlanDto
                {
                    Id = "lifetime",
                    Name = "Lifetime Celebration",
                    Price = 69.99m,
                    Currency = "USD",
                    Interval = "lifetime",
                    Savings = "One-time payment, celebrate forever!",
                    Description = "Eternal love & premium features for your bird",
                    CharityAllocation = 20,
                    Features = new[]
                    {
                        "All premium features forever",
                        "Custom profile theme & cover",
                        "Highlighted Best Moments",
                        "'Celebrated Bird' badge",
                        "Unlimited photos & videos",
                        "Memory collages & story albums",
                        "QR code for profile sharing",
                        "Pin up to 5 story highlights",
                        "Donation tracker display",
                        "Exclusive lifetime badge",
                        "Support bird charities",
                        "VIP support access"
                    }
                }
            };

            return Ok(plans);
        }
    }
}
