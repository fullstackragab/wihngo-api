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
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly IPasswordValidationService _passwordValidation;
        private readonly IAuthEmailService _authEmailService;
        private readonly ILogger<AuthController> _logger;
        
        // Configuration
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 30;
        private const int RefreshTokenExpiryDays = 30;

        public AuthController(
            AppDbContext db, 
            IMapper mapper, 
            ITokenService tokenService,
            IPasswordValidationService passwordValidation,
            IAuthEmailService authEmailService,
            ILogger<AuthController> logger)
        {
            _db = db;
            _mapper = mapper;
            _tokenService = tokenService;
            _passwordValidation = passwordValidation;
            _authEmailService = authEmailService;
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

                // Validate password strength
                var (isValid, errors) = _passwordValidation.ValidatePassword(dto.Password);
                if (!isValid)
                {
                    return BadRequest(new { message = "Password does not meet security requirements", errors });
                }

                // Check if email already exists
                var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
                if (existing != null)
                {
                    return Conflict(new { message = "Email already registered" });
                }

                var user = _mapper.Map<User>(dto);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
                user.UserId = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.EmailConfirmed = false;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                
                // Generate email confirmation token
                user.EmailConfirmationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Generate tokens
                var (token, expiresAt) = _tokenService.GenerateToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                user.RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
                user.LastLoginAt = DateTime.UtcNow;
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("User registered: {Email}", dto.Email);

                // Send email confirmation email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendEmailConfirmationAsync(user.Email, user.Name, user.EmailConfirmationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                    }
                });

                var resp = new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed
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

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent email: {Email}", dto.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check if account is locked
                if (user.IsAccountLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                    _logger.LogWarning("Login attempt for locked account: {Email}", dto.Email);
                    return Unauthorized(new 
                    { 
                        message = $"Account is locked due to too many failed login attempts. Try again in {remainingMinutes} minutes.",
                        code = "ACCOUNT_LOCKED",
                        lockoutEnd = user.LockoutEnd.Value
                    });
                }

                // Unlock account if lockout period has expired
                if (user.IsAccountLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow)
                {
                    user.IsAccountLocked = false;
                    user.LockoutEnd = null;
                    user.FailedLoginAttempts = 0;
                    await _db.SaveChangesAsync();
                }

                // Verify password
                var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!valid)
                {
                    // Increment failed login attempts
                    user.FailedLoginAttempts++;
                    
                    if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        user.IsAccountLocked = true;
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                        _logger.LogWarning("Account locked due to too many failed attempts: {Email}", dto.Email);
                    }
                    
                    await _db.SaveChangesAsync();
                
                    _logger.LogWarning("Failed login attempt for email: {Email}. Attempts: {Attempts}", 
                        dto.Email, user.FailedLoginAttempts);
                
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Successful login - reset failed attempts
                user.FailedLoginAttempts = 0;
                user.LastLoginAt = DateTime.UtcNow;

                // Generate new tokens
                var (token, expiresAt) = _tokenService.GenerateToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                user.RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("User logged in: {Email}", dto.Email);

                var resp = new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", dto.Email);
                return StatusCode(500, new { message = "Login failed. Please try again." });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                var hashedToken = _tokenService.HashRefreshToken(dto.RefreshToken);
                
                var user = await _db.Users.FirstOrDefaultAsync(u => 
                    u.RefreshTokenHash == hashedToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                // Check if account is locked
                if (user.IsAccountLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Account is locked", code = "ACCOUNT_LOCKED" });
                }

                // Generate new tokens
                var (newToken, expiresAt) = _tokenService.GenerateToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                
                user.RefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Token refreshed for user: {UserId}", user.UserId);

                return Ok(new RefreshTokenResponseDto
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return StatusCode(500, new { message = "Token refresh failed" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized();
                }

                var user = await _db.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return NotFound();
                }

                // Revoke refresh token
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiry = null;
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("User logged out: {UserId}", userGuid);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return StatusCode(500, new { message = "Logout failed" });
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
                var emailConfirmed = User.FindFirstValue("email_confirmed") == "true";

                return Ok(new
                {
                    valid = true,
                    userId,
                    email,
                    name,
                    emailConfirmed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation error");
                return Unauthorized(new { valid = false, message = "Invalid token" });
            }
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == dto.Email.ToLower() &&
                    u.EmailConfirmationToken == dto.Token &&
                    u.EmailConfirmationTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid or expired confirmation token" });
                }

                user.EmailConfirmed = true;
                user.EmailConfirmationToken = null;
                user.EmailConfirmationTokenExpiry = null;
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Email confirmed for user: {Email}", user.Email);

                // Send welcome email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendWelcomeEmailAsync(user.Email, user.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "Email confirmed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email confirmation error");
                return StatusCode(500, new { message = "Email confirmation failed" });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
                
                // Don't reveal if user exists - always return success
                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                    return Ok(new { message = "If the email exists, a password reset link has been sent." });
                }

                // Generate reset token
                user.PasswordResetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Password reset token generated for: {Email}", user.Email);

                // Send password reset email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendPasswordResetAsync(user.Email, user.Name, user.PasswordResetToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password error");
                return StatusCode(500, new { message = "Password reset request failed" });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                // Validate password strength
                var (isValid, errors) = _passwordValidation.ValidatePassword(dto.NewPassword);
                if (!isValid)
                {
                    return BadRequest(new { message = "Password does not meet security requirements", errors });
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == dto.Email.ToLower() &&
                    u.PasswordResetToken == dto.Token &&
                    u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset token" });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                
                // Revoke all refresh tokens for security
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiry = null;
                
                // Reset failed login attempts
                user.FailedLoginAttempts = 0;
                user.IsAccountLocked = false;
                user.LockoutEnd = null;
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for user: {Email}", user.Email);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            user.Email, 
                            user.Name, 
                            "Password Changed", 
                            "Your password was successfully reset. If you didn't make this change, please contact support immediately.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset error");
                return StatusCode(500, new { message = "Password reset failed" });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized();
                }

                var user = await _db.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return NotFound();
                }

                // Verify current password
                var valid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
                if (!valid)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                // Validate new password strength
                var (isValid, errors) = _passwordValidation.ValidatePassword(dto.NewPassword);
                if (!isValid)
                {
                    return BadRequest(new { message = "New password does not meet security requirements", errors });
                }

                // Check if new password is same as current
                if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "New password must be different from current password" });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                user.LastPasswordChangeAt = DateTime.UtcNow;
                
                // Revoke all refresh tokens for security
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiry = null;
                
                await _db.SaveChangesAsync();

                _logger.LogInformation("Password changed for user: {UserId}", userGuid);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            user.Email, 
                            user.Name, 
                            "Password Changed", 
                            "Your password was successfully changed. If you didn't make this change, please contact support immediately and reset your password.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "Password changed successfully. Please login again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change error");
                return StatusCode(500, new { message = "Password change failed" });
            }
        }
    }
}
