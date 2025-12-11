using System.Security.Claims;
using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Services;

namespace Wihngo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext db, IMapper mapper, TokenService tokenService, ILogger<AuthController> logger)
        {
            _db = db;
            _mapper = mapper;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet]
        public string Get() => "Auth API is running";

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existing != null)
                {
                    return Conflict(new { message = "Email already registered" });
                }

                var user = _mapper.Map<User>(dto);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                user.UserId = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User registered: {Email}", dto.Email);

                var resp = new AuthResponseDto
                {
                    Token = token,
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for email: {Email}", dto.Email);
                return StatusCode(500, new { message = "Registration failed. Please try again." });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!valid)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User logged in: {Email}", dto.Email);

                var resp = new AuthResponseDto
                {
                    Token = token,
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", dto.Email);
                return StatusCode(500, new { message = "Login failed. Please try again." });
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);
                var name = User.FindFirstValue(ClaimTypes.Name);

                return Ok(new
                {
                    valid = true,
                    userId,
                    email,
                    name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation error");
                return Unauthorized(new { valid = false, message = "Invalid token" });
            }
        }
    }
}
