namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Wihngo.Data;
    using Wihngo.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class BirdsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BirdsController(AppDbContext db)
        {
            _db = db;
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
