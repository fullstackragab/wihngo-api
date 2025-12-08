namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class BirdsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public BirdsController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BirdSummaryDto>>> Get()
        {
            // Project only necessary fields and counts to avoid loading all support transaction details
            var birds = await _db.Birds
                .AsNoTracking()
                .Select(b => new BirdSummaryDto
                {
                    BirdId = b.BirdId,
                    Name = b.Name,
                    Species = b.Species,
                    ImageUrl = b.ImageUrl,
                    Tagline = b.Tagline,
                    LovedBy = b.LovedCount,
                    SupportedBy = b.SupportTransactions.Count(),
                    OwnerId = b.OwnerId
                })
                .ToListAsync();

            return Ok(birds);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BirdProfileDto>> Get(Guid id)
        {
            var bird = await _db.Birds
                .AsNoTracking()
                .Include(b => b.Owner)
                .Include(b => b.Stories)
                .Include(b => b.SupportTransactions)
                .FirstOrDefaultAsync(b => b.BirdId == id);

            if (bird == null) return NotFound();

            var dto = _mapper.Map<BirdProfileDto>(bird);

            // Fill counts explicitly
            dto.LovedBy = bird.LovedCount;
            dto.SupportedBy = bird.SupportTransactions?.Count ?? 0;
            dto.ImageUrl = bird.ImageUrl;

            // Map owner summary
            if (bird.Owner != null)
            {
                dto.Owner = new UserSummaryDto { UserId = bird.Owner.UserId, Name = bird.Owner.Name };
            }

            // Populate personality/fun facts/conservation if separate tables exist
            // (left as placeholders if not present)
            dto.Personality = dto.Personality ?? new System.Collections.Generic.List<string>
            {
                "Fearless and territorial",
                "Incredibly vocal for their size",
                "Early risers who sing before dawn",
                "Devoted parents"
            };

            dto.FunFacts = dto.FunFacts ?? new System.Collections.Generic.List<string>
            {
                "Males perform spectacular dive displays, reaching speeds of 60 mph",
                "They can remember every flower they've visited",
                "Their heart beats up to 1,260 times per minute",
                "They're one of the few hummingbirds that sing"
            };

            dto.Conservation ??= new ConservationDto
            {
                Status = "Least Concern",
                Needs = "Native plant gardens, year-round nectar sources, pesticide-free habitats"
            };

            return Ok(dto);
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<BirdProfileDto>> Profile(Guid id)
        {
            // Reuse existing profile logic
            return await Get(id);
        }

        [HttpPost]
        public async Task<ActionResult<Bird>> Post([FromBody] Bird bird)
        {
            bird.BirdId = Guid.NewGuid();
            bird.CreatedAt = DateTime.UtcNow;
            _db.Birds.Add(bird);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = bird.BirdId }, bird);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] Bird updated)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            bird.Name = updated.Name;
            bird.Species = updated.Species;
            bird.Description = updated.Description;
            bird.ImageUrl = updated.ImageUrl;
            bird.Tagline = updated.Tagline;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            _db.Birds.Remove(bird);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/love")]
        public async Task<IActionResult> Love(Guid id)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized("User not found");

            // Check existing love
            var exists = await _db.Loves.FindAsync(userId, id);
            if (exists != null) return BadRequest("Already loved");

            var love = new Love { UserId = userId, BirdId = id };
            _db.Loves.Add(love);

            // Atomic increment
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = loved_count + 1 WHERE bird_id = {id}");
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/unlove")]
        public async Task<IActionResult> Unlove(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var love = await _db.Loves.FindAsync(userId, id);
            if (love == null) return NotFound();

            // Atomic decrement but not below zero
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = {id}");
            _db.Loves.Remove(love);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/lovers")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> Lovers(Guid id)
        {
            var lovers = await _db.Loves
                .Where(l => l.BirdId == id)
                .Include(l => l.User)
                .Select(l => new UserSummaryDto { UserId = l.UserId, Name = l.User != null ? l.User.Name : string.Empty })
                .ToListAsync();

            return Ok(lovers);
        }

        [Authorize]
        [HttpPost("{id}/support-usage")]
        public async Task<ActionResult<SupportUsageDto>> ReportSupportUsage(Guid id, [FromBody] SupportUsageDto dto)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var callerId)) return Unauthorized();

            // Only owner can report support usage
            if (bird.OwnerId != callerId) return Forbid();

            var usage = new SupportUsage
            {
                UsageId = Guid.NewGuid(),
                BirdId = id,
                ReportedBy = callerId,
                Amount = dto.Amount,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _db.Add(usage);
            await _db.SaveChangesAsync();

            dto.UsageId = usage.UsageId;
            dto.BirdId = usage.BirdId;
            dto.ReportedBy = usage.ReportedBy;
            dto.CreatedAt = usage.CreatedAt;

            return CreatedAtAction(nameof(ReportSupportUsage), new { id = id, usageId = usage.UsageId }, dto);
        }
    }
}
