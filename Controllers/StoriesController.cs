namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Wihngo.Data;
    using Wihngo.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StoriesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Story>>> Get()
        {
            return await _db.Stories.Include(s => s.Bird).Include(s => s.Author).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Story>> Get(Guid id)
        {
            var story = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null) return NotFound();
            return story;
        }

        [HttpPost]
        public async Task<ActionResult<Story>> Post([FromBody] Story story)
        {
            story.StoryId = Guid.NewGuid();
            story.CreatedAt = DateTime.UtcNow;
            _db.Stories.Add(story);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = story.StoryId }, story);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] Story updated)
        {
            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            story.Content = updated.Content;
            story.ImageUrl = updated.ImageUrl;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
