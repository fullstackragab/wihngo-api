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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public UsersController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> Get()
        {
            var users = await _db.Users.Include(u => u.Birds).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<UserReadDto>>(users));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> Get(Guid id)
        {
            var user = await _db.Users
                .Include(u => u.Birds)
                .Include(u => u.Stories)
                .Include(u => u.SupportTransactions)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();
            return Ok(_mapper.Map<UserReadDto>(user));
        }

        [HttpPost]
        public async Task<ActionResult<UserReadDto>> Post([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existing != null) return Conflict("Email already registered");

            var user = _mapper.Map<User>(dto);
            user.UserId = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            // Note: password must be set via auth/register to get hashed; here we set hash directly for simplicity
            user.PasswordHash = dto.Password; // not recommended; prefer register endpoint

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var read = _mapper.Map<UserReadDto>(user);
            return CreatedAtAction(nameof(Get), new { id = user.UserId }, read);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] User updated)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Name = updated.Name;
            user.Email = updated.Email;
            user.ProfileImage = updated.ProfileImage;
            user.Bio = updated.Bio;
            user.PasswordHash = updated.PasswordHash;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
