namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext db, IMapper mapper, ILogger<UsersController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> Get()
        {
            var users = await _db.Users
                .AsNoTracking()
                .ToListAsync();
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

        [HttpGet("profile/{id}")]
        public async Task<ActionResult<UserProfileDto>> Profile(Guid id)
        {
            var user = await _db.Users
                .Include(u => u.Birds)
                .Include(u => u.Stories)
                .Include(u => u.SupportTransactions)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            // Build profile DTO
            var profile = new UserProfileDto
            {
                Name = user.Name,
                Location = "", // not stored yet
                JoinedDate = user.CreatedAt.ToString("MMMM yyyy"),
                Bio = user.Bio ?? string.Empty,
                Avatar = "??",
                Stats = new ProfileStats
                {
                    BirdsLoved = user.Birds.Count, // placeholder: count of owned birds
                    StoriesShared = user.Stories.Count,
                    Supported = user.SupportTransactions.Count
                },
                FavoriteBirds = user.Birds.Select(b => new FavoriteBirdDto
                {
                    Name = b.Name,
                    Emoji = "??",
                    Loved = true
                }).ToList(),
                RecentStories = user.Stories.OrderByDescending(s => s.CreatedAt).Take(5).Select(s => new StorySummaryDto
                {
                    StoryId = s.StoryId,
                    Title = s.Content.Length > 30 ? s.Content.Substring(0, 30) + "..." : s.Content,
                    Bird = s.Bird?.Name ?? string.Empty,
                    Date = s.CreatedAt.ToString("MMMM d, yyyy"),
                    Preview = s.Content.Length > 140 ? s.Content.Substring(0, 140) + "..." : s.Content
                }).ToList()
            };

            return Ok(profile);
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

        /// <summary>
        /// Register or update push token for user
        /// POST /api/users/{userId}/push-token
        /// </summary>
        [HttpPost("{userId}/push-token")]
        [Authorize]
        public async Task<IActionResult> RegisterPushToken(Guid userId, [FromBody] RegisterPushTokenRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                // Verify the user is updating their own push token
                if (userId != authenticatedUserId)
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(request.PushToken))
                {
                    return BadRequest(new { message = "Push token is required" });
                }

                // Check if device already exists
                var existingDevice = await _db.UserDevices
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.PushToken == request.PushToken);

                if (existingDevice != null)
                {
                    // Update existing device
                    existingDevice.DeviceType = request.DeviceType ?? existingDevice.DeviceType;
                    existingDevice.DeviceName = request.DeviceName ?? existingDevice.DeviceName;
                    existingDevice.IsActive = true;
                    existingDevice.LastUsedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new device
                    var device = new UserDevice
                    {
                        UserId = userId,
                        PushToken = request.PushToken,
                        DeviceType = request.DeviceType ?? "unknown",
                        DeviceName = request.DeviceName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow
                    };

                    _db.UserDevices.Add(device);
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Registered push token for user {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Push token registered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register push token for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to register push token", error = ex.Message });
            }
        }
    }

    public class RegisterPushTokenRequest
    {
        public string PushToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }
}
