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
    public class StoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public StoriesController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<StorySummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var total = await _db.Stories.LongCountAsync();

            var items = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtoItems = _mapper.Map<IEnumerable<StorySummaryDto>>(items);

            var result = new PagedResult<StorySummaryDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = dtoItems
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StoryReadDto>> Get(Guid id)
        {
            var story = await _db.Stories
                .Include(s => s.Bird)
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null) return NotFound();
            return Ok(_mapper.Map<StoryReadDto>(story));
        }

        [HttpPost]
        public async Task<ActionResult<StoryReadDto>> Post([FromBody] Story story)
        {
            story.StoryId = Guid.NewGuid();
            story.CreatedAt = DateTime.UtcNow;
            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            var created = await _db.Stories.Include(s => s.Bird).Include(s => s.Author).FirstOrDefaultAsync(s => s.StoryId == story.StoryId);
            return CreatedAtAction(nameof(Get), new { id = story.StoryId }, _mapper.Map<StoryReadDto>(created));
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
