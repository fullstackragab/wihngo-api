namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Wihngo.Dtos;
    using Wihngo.Services.Interfaces;

    [ApiController]
    [Route("api/charity")]
    public class CharityController : ControllerBase
    {
        private readonly ICharityService _charityService;
        private readonly ILogger<CharityController> _logger;

        public CharityController(ICharityService charityService, ILogger<CharityController> logger)
        {
            _charityService = charityService;
            _logger = logger;
        }

        /// <summary>
        /// Get charity impact stats for a specific bird
        /// GET /api/charity/impact/{birdId}
        /// </summary>
        [HttpGet("impact/{birdId}")]
        [AllowAnonymous]
        public async Task<ActionResult<CharityImpactDto>> GetBirdImpact(Guid birdId)
        {
            try
            {
                var impact = await _charityService.GetBirdCharityImpactAsync(birdId);
                return Ok(impact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charity impact for bird {BirdId}", birdId);
                return StatusCode(500, new { message = "An error occurred while retrieving charity impact" });
            }
        }

        /// <summary>
        /// Get global charity impact stats
        /// GET /api/charity/impact/global
        /// </summary>
        [HttpGet("impact/global")]
        [AllowAnonymous]
        public async Task<ActionResult<GlobalCharityImpactDto>> GetGlobalImpact()
        {
            try
            {
                var impact = await _charityService.GetGlobalCharityImpactAsync();
                return Ok(impact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global charity impact");
                return StatusCode(500, new { message = "An error occurred while retrieving global charity impact" });
            }
        }

        /// <summary>
        /// Get list of supported charities
        /// GET /api/charity/partners
        /// </summary>
        [HttpGet("partners")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<CharityPartnerDto>> GetPartners()
        {
            var partners = new[]
            {
                new CharityPartnerDto
                {
                    Name = "Local Bird Shelter Network",
                    Description = "Rescue and rehabilitation services",
                    Website = "https://birdshelternet.org"
                },
                new CharityPartnerDto
                {
                    Name = "Avian Conservation Fund",
                    Description = "Species protection and habitat preservation",
                    Website = "https://avianconservation.org"
                },
                new CharityPartnerDto
                {
                    Name = "Wildlife Rescue Alliance",
                    Description = "Emergency veterinary care for birds",
                    Website = "https://wildliferescue.org"
                }
            };

            return Ok(partners);
        }
    }
}
