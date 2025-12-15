using System.Security.Claims;
using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
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
        private readonly IDbConnectionFactory _dbFactory;
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
            IDbConnectionFactory dbFactory, 
            IMapper mapper, 
            ITokenService tokenService,
            IPasswordValidationService passwordValidation,
            IAuthEmailService authEmailService,
            ILogger<AuthController> logger)
        {
            _dbFactory = dbFactory;
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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();

                // Check if email already exists
                var existingId = await connection.ExecuteScalarAsync<Guid?>(
                    "SELECT user_id FROM users WHERE LOWER(email) = LOWER(@Email)",
                    new { Email = dto.Email });
                
                if (existingId != null)
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

                // Generate tokens
                var (token, expiresAt) = _tokenService.GenerateToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                user.RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
                user.LastLoginAt = DateTime.UtcNow;

                // Insert user with Dapper
                await connection.ExecuteAsync(@"
                    INSERT INTO users (user_id, name, email, password_hash, profile_image, bio, created_at, 
                                       email_confirmed, email_confirmation_token, email_confirmation_token_expiry,
                                       is_account_locked, failed_login_attempts, last_login_at,
                                       refresh_token_hash, refresh_token_expiry, last_password_change_at)
                    VALUES (@UserId, @Name, @Email, @PasswordHash, @ProfileImage, @Bio, @CreatedAt,
                            @EmailConfirmed, @EmailConfirmationToken, @EmailConfirmationTokenExpiry,
                            @IsAccountLocked, @FailedLoginAttempts, @LastLoginAt,
                            @RefreshTokenHash, @RefreshTokenExpiry, @LastPasswordChangeAt)",
                    new
                    {
                        user.UserId,
                        user.Name,
                        user.Email,
                        user.PasswordHash,
                        user.ProfileImage,
                        user.Bio,
                        user.CreatedAt,
                        user.EmailConfirmed,
                        user.EmailConfirmationToken,
                        user.EmailConfirmationTokenExpiry,
                        user.IsAccountLocked,
                        user.FailedLoginAttempts,
                        user.LastLoginAt,
                        user.RefreshTokenHash,
                        user.RefreshTokenExpiry,
                        user.LastPasswordChangeAt
                    });

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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();

                // Get user with Dapper
                var user = await connection.QueryFirstOrDefaultAsync<User>(@"
                    SELECT user_id, name, email, password_hash, profile_image, bio, created_at,
                           email_confirmed, is_account_locked, failed_login_attempts, lockout_end,
                           last_login_at, last_password_change_at
                    FROM users
                    WHERE LOWER(email) = LOWER(@Email)",
                    new { Email = dto.Email });
                    
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
                    await connection.ExecuteAsync(@"
                        UPDATE users 
                        SET is_account_locked = false, lockout_end = NULL, failed_login_attempts = 0
                        WHERE user_id = @UserId",
                        new { user.UserId });
                    
                    user.IsAccountLocked = false;
                    user.LockoutEnd = null;
                    user.FailedLoginAttempts = 0;
                }

                // Verify password
                var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!valid)
                {
                    // Increment failed login attempts
                    user.FailedLoginAttempts++;
                    
                    bool shouldLock = user.FailedLoginAttempts >= MaxFailedAttempts;
                    if (shouldLock)
                    {
                        user.IsAccountLocked = true;
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                        _logger.LogWarning("Account locked due to too many failed attempts: {Email}", dto.Email);
                        
                        await connection.ExecuteAsync(@"
                            UPDATE users 
                            SET failed_login_attempts = @FailedLoginAttempts, 
                                is_account_locked = true, 
                                lockout_end = @LockoutEnd
                            WHERE user_id = @UserId",
                            new { user.FailedLoginAttempts, user.LockoutEnd, user.UserId });
                    }
                    else
                    {
                        await connection.ExecuteAsync(@"
                            UPDATE users 
                            SET failed_login_attempts = @FailedLoginAttempts
                            WHERE user_id = @UserId",
                            new { user.FailedLoginAttempts, user.UserId });
                    }
                
                    _logger.LogWarning("Failed login attempt for email: {Email}. Attempts: {Attempts}", 
                        dto.Email, user.FailedLoginAttempts);
                
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Successful login - reset failed attempts
                var (token, expiresAt) = _tokenService.GenerateToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET failed_login_attempts = 0, 
                        last_login_at = @LastLoginAt,
                        refresh_token_hash = @RefreshTokenHash, 
                        refresh_token_expiry = @RefreshTokenExpiry
                    WHERE user_id = @UserId",
                    new
                    {
                        LastLoginAt = DateTime.UtcNow,
                        RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken),
                        RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                        user.UserId
                    });

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
                
                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                var user = await connection.QueryFirstOrDefaultAsync<User>(@"
                    SELECT user_id, name, email, password_hash, profile_image, bio, created_at,
                           email_confirmed, is_account_locked, lockout_end
                    FROM users
                    WHERE refresh_token_hash = @Hash AND refresh_token_expiry > @Now",
                    new { Hash = hashedToken, Now = DateTime.UtcNow });

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
                
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET refresh_token_hash = @Hash, refresh_token_expiry = @Expiry
                    WHERE user_id = @UserId",
                    new
                    {
                        Hash = _tokenService.HashRefreshToken(newRefreshToken),
                        Expiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                        user.UserId
                    });

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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                var rowsAffected = await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET refresh_token_hash = NULL, refresh_token_expiry = NULL
                    WHERE user_id = @UserId",
                    new { UserId = userGuid });
                
                if (rowsAffected == 0)
                {
                    return NotFound();
                }

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
                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Find user with matching token
                var userInfo = await connection.QueryFirstOrDefaultAsync<(string Name, string Email)>(@"
                    SELECT name, email
                    FROM users
                    WHERE LOWER(email) = LOWER(@Email) 
                      AND email_confirmation_token = @Token 
                      AND email_confirmation_token_expiry > @Now",
                    new { dto.Email, dto.Token, Now = DateTime.UtcNow });

                if (userInfo.Name == null || userInfo.Email == null)
                {
                    return BadRequest(new { message = "Invalid or expired confirmation token" });
                }

                // Update user
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET email_confirmed = true, 
                        email_confirmation_token = NULL, 
                        email_confirmation_token_expiry = NULL
                    WHERE LOWER(email) = LOWER(@Email) AND email_confirmation_token = @Token",
                    new { dto.Email, dto.Token });

                _logger.LogInformation("Email confirmed for user: {Email}", userInfo.Email);

                // Send welcome email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendWelcomeEmailAsync(userInfo.Email, userInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", userInfo.Email);
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
                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Check if user exists
                var userInfo = await connection.QueryFirstOrDefaultAsync<(Guid UserId, string Name)>(
                    "SELECT user_id, name FROM users WHERE LOWER(email) = LOWER(@Email)",
                    new { dto.Email });
                
                // Don't reveal if user exists - always return success
                if (userInfo.UserId == Guid.Empty)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                    return Ok(new { message = "If the email exists, a password reset link has been sent." });
                }

                // Generate reset token
                var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var resetTokenExpiry = DateTime.UtcNow.AddHours(1);
                
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET password_reset_token = @Token, password_reset_token_expiry = @Expiry
                    WHERE user_id = @UserId",
                    new { Token = resetToken, Expiry = resetTokenExpiry, userInfo.UserId });

                _logger.LogInformation("Password reset token generated for: {Email}", dto.Email);

                // Send password reset email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendPasswordResetAsync(dto.Email, userInfo.Name, resetToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", dto.Email);
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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Find user with matching token
                var userInfo = await connection.QueryFirstOrDefaultAsync<(Guid UserId, string Name, string Email)>(@"
                    SELECT user_id, name, email
                    FROM users
                    WHERE LOWER(email) = LOWER(@Email) 
                      AND password_reset_token = @Token 
                      AND password_reset_token_expiry > @Now",
                    new { dto.Email, dto.Token, Now = DateTime.UtcNow });

                if (userInfo.UserId == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid or expired reset token" });
                }

                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET password_hash = @PasswordHash,
                        password_reset_token = NULL,
                        password_reset_token_expiry = NULL,
                        last_password_change_at = @LastChange,
                        refresh_token_hash = NULL,
                        refresh_token_expiry = NULL,
                        failed_login_attempts = 0,
                        is_account_locked = false,
                        lockout_end = NULL
                    WHERE user_id = @UserId",
                    new
                    {
                        PasswordHash = newPasswordHash,
                        LastChange = DateTime.UtcNow,
                        userInfo.UserId
                    });

                _logger.LogInformation("Password reset successful for user: {Email}", userInfo.Email);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            userInfo.Email, 
                            userInfo.Name, 
                            "Password Changed", 
                            "Your password was successfully reset. If you didn't make this change, please contact support immediately.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", userInfo.Email);
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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Get user
                var userInfo = await connection.QueryFirstOrDefaultAsync<(string PasswordHash, string Email, string Name)>(
                    "SELECT password_hash, email, name FROM users WHERE user_id = @UserId",
                    new { UserId = userGuid });
                
                if (userInfo.PasswordHash == null)
                {
                    return NotFound();
                }

                // Verify current password
                var valid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, userInfo.PasswordHash);
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
                if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, userInfo.PasswordHash))
                {
                    return BadRequest(new { message = "New password must be different from current password" });
                }

                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                
                await connection.ExecuteAsync(@"
                    UPDATE users 
                    SET password_hash = @PasswordHash,
                        last_password_change_at = @LastChange,
                        refresh_token_hash = NULL,
                        refresh_token_expiry = NULL
                    WHERE user_id = @UserId",
                    new
                    {
                        PasswordHash = newPasswordHash,
                        LastChange = DateTime.UtcNow,
                        UserId = userGuid
                    });

                _logger.LogInformation("Password changed for user: {UserId}", userGuid);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            userInfo.Email, 
                            userInfo.Name, 
                            "Password Changed", 
                            "Your password was successfully changed. If you didn't make this change, please contact support immediately and reset your password.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", userInfo.Email);
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
