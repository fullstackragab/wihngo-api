namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
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
        public async Task<ActionResult<IEnumerable<Bird>>> Get()
        {
            return await _db.Birds.Include(b => b.Owner).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Bird>> Get(Guid id)
        {
            var bird = await _db.Birds
                .Include(b => b.Owner)
                .Include(b => b.Stories)
                .Include(b => b.SupportTransactions)
                .FirstOrDefaultAsync(b => b.BirdId == id);

            if (bird == null) return NotFound();
            return bird;
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<BirdProfileDto>> Profile(Guid id)
        {
            var bird = await _db.Birds
                .Include(b => b.Owner)
                .Include(b => b.Stories)
                .Include(b => b.SupportTransactions)
                .FirstOrDefaultAsync(b => b.BirdId == id);

            if (bird == null) return NotFound();

            var dto = _mapper.Map<BirdProfileDto>(bird);

            // Provide personality/fun facts/conservation placeholders (frontend-friendly)
            dto.Personality = new System.Collections.Generic.List<string>
            {
                "Fearless and territorial",
                "Incredibly vocal for their size",
                "Early risers who sing before dawn",
                "Devoted parents"
            };

            dto.FunFacts = new System.Collections.Generic.List<string>
            {
                "Males perform spectacular dive displays, reaching speeds of 60 mph",
                "They can remember every flower they've visited",
                "Their heart beats up to 1,260 times per minute",
                "They're one of the few hummingbirds that sing"
            };

            dto.Conservation = new ConservationDto
            {
                Status = "Least Concern",
                Needs = "Native plant gardens, year-round nectar sources, pesticide-free habitats"
            };

            return Ok(dto);
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
    }
}
