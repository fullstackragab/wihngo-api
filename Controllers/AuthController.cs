using System.Security.Claims;
using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT user_id FROM users WHERE LOWER(email) = LOWER(@email)";
                checkCmd.Parameters.AddWithValue("email", dto.Email);
                var existingId = await checkCmd.ExecuteScalarAsync();
                
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

                // Insert user
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO users (user_id, name, email, password_hash, profile_image, bio, created_at, 
                                       email_confirmed, email_confirmation_token, email_confirmation_token_expiry,
                                       is_account_locked, failed_login_attempts, last_login_at,
                                       refresh_token_hash, refresh_token_expiry, last_password_change_at)
                    VALUES (@user_id, @name, @email, @password_hash, @profile_image, @bio, @created_at,
                            @email_confirmed, @email_confirmation_token, @email_confirmation_token_expiry,
                            @is_account_locked, @failed_login_attempts, @last_login_at,
                            @refresh_token_hash, @refresh_token_expiry, @last_password_change_at)
                ";
                insertCmd.Parameters.AddWithValue("user_id", user.UserId);
                insertCmd.Parameters.AddWithValue("name", user.Name);
                insertCmd.Parameters.AddWithValue("email", user.Email);
                insertCmd.Parameters.AddWithValue("password_hash", user.PasswordHash);
                insertCmd.Parameters.AddWithValue("profile_image", (object?)user.ProfileImage ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("bio", (object?)user.Bio ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("created_at", user.CreatedAt);
                insertCmd.Parameters.AddWithValue("email_confirmed", user.EmailConfirmed);
                insertCmd.Parameters.AddWithValue("email_confirmation_token", (object?)user.EmailConfirmationToken ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("email_confirmation_token_expiry", (object?)user.EmailConfirmationTokenExpiry ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("is_account_locked", user.IsAccountLocked);
                insertCmd.Parameters.AddWithValue("failed_login_attempts", user.FailedLoginAttempts);
                insertCmd.Parameters.AddWithValue("last_login_at", (object?)user.LastLoginAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("refresh_token_hash", (object?)user.RefreshTokenHash ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("refresh_token_expiry", (object?)user.RefreshTokenExpiry ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("last_password_change_at", (object?)user.LastPasswordChangeAt ?? DBNull.Value);
                
                await insertCmd.ExecuteNonQueryAsync();

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

                // Get user
                var user = await GetUserByEmailAsync(connection, dto.Email);
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
                    using var unlockCmd = connection.CreateCommand();
                    unlockCmd.CommandText = @"
                        UPDATE users 
                        SET is_account_locked = false, lockout_end = NULL, failed_login_attempts = 0
                        WHERE user_id = @user_id
                    ";
                    unlockCmd.Parameters.AddWithValue("user_id", user.UserId);
                    await unlockCmd.ExecuteNonQueryAsync();
                    
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
                    }
                    
                    using var failCmd = connection.CreateCommand();
                    if (shouldLock)
                    {
                        failCmd.CommandText = @"
                            UPDATE users 
                            SET failed_login_attempts = @attempts, is_account_locked = true, lockout_end = @lockout_end
                            WHERE user_id = @user_id
                        ";
                        failCmd.Parameters.AddWithValue("lockout_end", user.LockoutEnd!.Value);
                    }
                    else
                    {
                        failCmd.CommandText = @"
                            UPDATE users 
                            SET failed_login_attempts = @attempts
                            WHERE user_id = @user_id
                        ";
                    }
                    failCmd.Parameters.AddWithValue("attempts", user.FailedLoginAttempts);
                    failCmd.Parameters.AddWithValue("user_id", user.UserId);
                    await failCmd.ExecuteNonQueryAsync();
                
                    _logger.LogWarning("Failed login attempt for email: {Email}. Attempts: {Attempts}", 
                        dto.Email, user.FailedLoginAttempts);
                
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Successful login - reset failed attempts
                var (token, expiresAt) = _tokenService.GenerateToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                using var successCmd = connection.CreateCommand();
                successCmd.CommandText = @"
                    UPDATE users 
                    SET failed_login_attempts = 0, last_login_at = @last_login_at,
                        refresh_token_hash = @refresh_token_hash, refresh_token_expiry = @refresh_token_expiry
                    WHERE user_id = @user_id
                ";
                successCmd.Parameters.AddWithValue("last_login_at", DateTime.UtcNow);
                successCmd.Parameters.AddWithValue("refresh_token_hash", _tokenService.HashRefreshToken(refreshToken));
                successCmd.Parameters.AddWithValue("refresh_token_expiry", DateTime.UtcNow.AddDays(RefreshTokenExpiryDays));
                successCmd.Parameters.AddWithValue("user_id", user.UserId);
                await successCmd.ExecuteNonQueryAsync();

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
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT user_id, name, email, password_hash, profile_image, bio, created_at,
                           email_confirmed, is_account_locked, lockout_end
                    FROM users
                    WHERE refresh_token_hash = @hash AND refresh_token_expiry > @now
                ";
                cmd.Parameters.AddWithValue("hash", hashedToken);
                cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

                User? user = null;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    user = ReadUserFromReader(reader);
                }

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
                
                await reader.CloseAsync();
                
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET refresh_token_hash = @hash, refresh_token_expiry = @expiry
                    WHERE user_id = @user_id
                ";
                updateCmd.Parameters.AddWithValue("hash", _tokenService.HashRefreshToken(newRefreshToken));
                updateCmd.Parameters.AddWithValue("expiry", DateTime.UtcNow.AddDays(RefreshTokenExpiryDays));
                updateCmd.Parameters.AddWithValue("user_id", user.UserId);
                await updateCmd.ExecuteNonQueryAsync();

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
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE users 
                    SET refresh_token_hash = NULL, refresh_token_expiry = NULL
                    WHERE user_id = @user_id
                ";
                cmd.Parameters.AddWithValue("user_id", userGuid);
                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
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
                using var findCmd = connection.CreateCommand();
                findCmd.CommandText = @"
                    SELECT user_id, name, email
                    FROM users
                    WHERE LOWER(email) = LOWER(@email) 
                      AND email_confirmation_token = @token 
                      AND email_confirmation_token_expiry > @now
                ";
                findCmd.Parameters.AddWithValue("email", dto.Email);
                findCmd.Parameters.AddWithValue("token", dto.Token);
                findCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
                
                string? userName = null;
                string? userEmail = null;
                using var reader = await findCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userName = reader.GetString(1);
                    userEmail = reader.GetString(2);
                }
                await reader.CloseAsync();

                if (userName == null || userEmail == null)
                {
                    return BadRequest(new { message = "Invalid or expired confirmation token" });
                }

                // Update user
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET email_confirmed = true, email_confirmation_token = NULL, email_confirmation_token_expiry = NULL
                    WHERE LOWER(email) = LOWER(@email) AND email_confirmation_token = @token
                ";
                updateCmd.Parameters.AddWithValue("email", dto.Email);
                updateCmd.Parameters.AddWithValue("token", dto.Token);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Email confirmed for user: {Email}", userEmail);

                // Send welcome email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendWelcomeEmailAsync(userEmail, userName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", userEmail);
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
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT user_id, name FROM users WHERE LOWER(email) = LOWER(@email)";
                checkCmd.Parameters.AddWithValue("email", dto.Email);
                
                Guid? userId = null;
                string? userName = null;
                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userId = reader.GetGuid(0);
                    userName = reader.GetString(1);
                }
                await reader.CloseAsync();
                
                // Don't reveal if user exists - always return success
                if (userId == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                    return Ok(new { message = "If the email exists, a password reset link has been sent." });
                }

                // Generate reset token
                var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var resetTokenExpiry = DateTime.UtcNow.AddHours(1);
                
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET password_reset_token = @token, password_reset_token_expiry = @expiry
                    WHERE user_id = @user_id
                ";
                updateCmd.Parameters.AddWithValue("token", resetToken);
                updateCmd.Parameters.AddWithValue("expiry", resetTokenExpiry);
                updateCmd.Parameters.AddWithValue("user_id", userId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Password reset token generated for: {Email}", dto.Email);

                // Send password reset email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendPasswordResetAsync(dto.Email, userName!, resetToken);
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
                using var findCmd = connection.CreateCommand();
                findCmd.CommandText = @"
                    SELECT user_id, name, email
                    FROM users
                    WHERE LOWER(email) = LOWER(@email) 
                      AND password_reset_token = @token 
                      AND password_reset_token_expiry > @now
                ";
                findCmd.Parameters.AddWithValue("email", dto.Email);
                findCmd.Parameters.AddWithValue("token", dto.Token);
                findCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
                
                Guid? userId = null;
                string? userName = null;
                string? userEmail = null;
                using var reader = await findCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userId = reader.GetGuid(0);
                    userName = reader.GetString(1);
                    userEmail = reader.GetString(2);
                }
                await reader.CloseAsync();

                if (userId == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset token" });
                }

                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET password_hash = @password_hash,
                        password_reset_token = NULL,
                        password_reset_token_expiry = NULL,
                        last_password_change_at = @last_change,
                        refresh_token_hash = NULL,
                        refresh_token_expiry = NULL,
                        failed_login_attempts = 0,
                        is_account_locked = false,
                        lockout_end = NULL
                    WHERE user_id = @user_id
                ";
                updateCmd.Parameters.AddWithValue("password_hash", newPasswordHash);
                updateCmd.Parameters.AddWithValue("last_change", DateTime.UtcNow);
                updateCmd.Parameters.AddWithValue("user_id", userId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Password reset successful for user: {Email}", userEmail);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            userEmail!, 
                            userName!, 
                            "Password Changed", 
                            "Your password was successfully reset. If you didn't make this change, please contact support immediately.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", userEmail);
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
                using var getCmd = connection.CreateCommand();
                getCmd.CommandText = "SELECT password_hash, email, name FROM users WHERE user_id = @user_id";
                getCmd.Parameters.AddWithValue("user_id", userGuid);
                
                string? currentPasswordHash = null;
                string? userEmail = null;
                string? userName = null;
                using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    currentPasswordHash = reader.GetString(0);
                    userEmail = reader.GetString(1);
                    userName = reader.GetString(2);
                }
                await reader.CloseAsync();
                
                if (currentPasswordHash == null)
                {
                    return NotFound();
                }

                // Verify current password
                var valid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentPasswordHash);
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
                if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, currentPasswordHash))
                {
                    return BadRequest(new { message = "New password must be different from current password" });
                }

                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
                
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET password_hash = @password_hash,
                        last_password_change_at = @last_change,
                        refresh_token_hash = NULL,
                        refresh_token_expiry = NULL
                    WHERE user_id = @user_id
                ";
                updateCmd.Parameters.AddWithValue("password_hash", newPasswordHash);
                updateCmd.Parameters.AddWithValue("last_change", DateTime.UtcNow);
                updateCmd.Parameters.AddWithValue("user_id", userGuid);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Password changed for user: {UserId}", userGuid);

                // Send security alert email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authEmailService.SendSecurityAlertAsync(
                            userEmail!, 
                            userName!, 
                            "Password Changed", 
                            "Your password was successfully changed. If you didn't make this change, please contact support immediately and reset your password.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send security alert email to {Email}", userEmail);
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

        // Helper methods
        private async Task<User?> GetUserByEmailAsync(NpgsqlConnection connection, string email)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT user_id, name, email, password_hash, profile_image, bio, created_at,
                       email_confirmed, is_account_locked, failed_login_attempts, lockout_end,
                       last_login_at, last_password_change_at
                FROM users
                WHERE LOWER(email) = LOWER(@email)
            ";
            cmd.Parameters.AddWithValue("email", email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUserFromReader(reader);
            }
            return null;
        }

        private User ReadUserFromReader(NpgsqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                ProfileImage = reader.IsDBNull(reader.GetOrdinal("profile_image")) ? null : reader.GetString(reader.GetOrdinal("profile_image")),
                Bio = reader.IsDBNull(reader.GetOrdinal("bio")) ? null : reader.GetString(reader.GetOrdinal("bio")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                EmailConfirmed = reader.GetBoolean(reader.GetOrdinal("email_confirmed")),
                IsAccountLocked = reader.GetBoolean(reader.GetOrdinal("is_account_locked")),
                FailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("failed_login_attempts")) ? 0 : reader.GetInt32(reader.GetOrdinal("failed_login_attempts")),
                LockoutEnd = reader.IsDBNull(reader.GetOrdinal("lockout_end")) ? null : reader.GetDateTime(reader.GetOrdinal("lockout_end")),
                LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_at")),
                LastPasswordChangeAt = reader.IsDBNull(reader.GetOrdinal("last_password_change_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_password_change_at"))
            };
        }
    }
}
