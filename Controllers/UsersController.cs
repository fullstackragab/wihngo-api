namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using Npgsql;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly IS3Service _s3Service;

        public UsersController(
            IDbConnectionFactory dbFactory, 
            IMapper mapper, 
            ILogger<UsersController> logger,
            IS3Service s3Service)
        {
            _dbFactory = dbFactory;
            _mapper = mapper;
            _logger = logger;
            _s3Service = s3Service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> Get()
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT user_id, name, email, profile_image, bio, created_at FROM users";
            
            var users = new List<User>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(ReadUserBasicFromReader(reader));
            }
            
            return Ok(_mapper.Map<IEnumerable<UserReadDto>>(users));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> Get(Guid id)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            // Get user with basic info
            var user = await GetUserByIdAsync(connection, id);
            if (user == null) return NotFound();
            
            // Get user's birds
            user.Birds = await GetUserBirdsAsync(connection, id);
            
            // Get user's stories
            user.Stories = await GetUserStoriesAsync(connection, id);
            
            // Get user's support transactions
            user.SupportTransactions = await GetUserSupportTransactionsAsync(connection, id);

            return Ok(_mapper.Map<UserReadDto>(user));
        }

        [HttpGet("profile/{id}")]
        public async Task<ActionResult<UserProfileDto>> Profile(Guid id)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            // Get user with basic info
            var user = await GetUserByIdAsync(connection, id);
            if (user == null) return NotFound();
            
            // Get user's birds
            user.Birds = await GetUserBirdsAsync(connection, id);
            
            // Get user's stories
            user.Stories = await GetUserStoriesAsync(connection, id);
            
            // Get user's support transactions
            user.SupportTransactions = await GetUserSupportTransactionsAsync(connection, id);

            // Get recent stories with story birds
            var recentStories = new List<StorySummaryDto>();
            using var storyCmd = connection.CreateCommand();
            storyCmd.CommandText = @"
                SELECT s.story_id, s.content, s.mode, s.created_at, s.image_url, s.video_url
                FROM stories s
                WHERE s.author_id = @author_id
                ORDER BY s.created_at DESC
                LIMIT 5
            ";
            storyCmd.Parameters.AddWithValue("author_id", id);
            
            using var storyReader = await storyCmd.ExecuteReaderAsync();
            while (await storyReader.ReadAsync())
            {
                var storyId = storyReader.GetGuid(0);
                var content = storyReader.GetString(1);
                var modeString = storyReader.GetString(2);
                var createdAt = storyReader.GetDateTime(3);
                var imageUrl = storyReader.IsDBNull(4) ? null : storyReader.GetString(4);
                var videoUrl = storyReader.IsDBNull(5) ? null : storyReader.GetString(5);
                
                // Parse mode string to enum
                StoryMode? mode = null;
                if (Enum.TryParse<StoryMode>(modeString, true, out var parsedMode))
                {
                    mode = parsedMode;
                }
                
                recentStories.Add(new StorySummaryDto
                {
                    StoryId = storyId,
                    Birds = new List<string>(), // Will be populated separately if needed
                    Mode = mode,
                    Date = createdAt.ToString("MMMM d, yyyy"),
                    Preview = content.Length > 140 ? content.Substring(0, 140) + "..." : content,
                    ImageS3Key = imageUrl,
                    VideoS3Key = videoUrl
                });
            }

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
                    BirdsLoved = user.Birds.Count,
                    StoriesShared = user.Stories.Count,
                    Supported = user.SupportTransactions.Count
                },
                FavoriteBirds = user.Birds.Select(b => new FavoriteBirdDto
                {
                    Name = b.Name,
                    Emoji = "??",
                    Loved = true
                }).ToList(),
                RecentStories = recentStories
            };

            return Ok(profile);
        }

        [HttpPost]
        public async Task<ActionResult<UserReadDto>> Post([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            // Check if email already exists
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT user_id FROM users WHERE email = @email";
            checkCmd.Parameters.AddWithValue("email", dto.Email);
            var existing = await checkCmd.ExecuteScalarAsync();
            
            if (existing != null) return Conflict("Email already registered");

            var user = _mapper.Map<User>(dto);
            user.UserId = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.PasswordHash = dto.Password; // not recommended; prefer register endpoint

            // Insert user
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO users (user_id, name, email, password_hash, profile_image, bio, created_at, email_confirmed)
                VALUES (@user_id, @name, @email, @password_hash, @profile_image, @bio, @created_at, false)
            ";
            insertCmd.Parameters.AddWithValue("user_id", user.UserId);
            insertCmd.Parameters.AddWithValue("name", user.Name);
            insertCmd.Parameters.AddWithValue("email", user.Email);
            insertCmd.Parameters.AddWithValue("password_hash", user.PasswordHash);
            insertCmd.Parameters.AddWithValue("profile_image", (object?)user.ProfileImage ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("bio", (object?)user.Bio ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("created_at", user.CreatedAt);
            await insertCmd.ExecuteNonQueryAsync();

            var read = _mapper.Map<UserReadDto>(user);
            return CreatedAtAction(nameof(Get), new { id = user.UserId }, read);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] User updated)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE users 
                SET name = @name, email = @email, profile_image = @profile_image, 
                    bio = @bio, password_hash = @password_hash
                WHERE user_id = @user_id
            ";
            cmd.Parameters.AddWithValue("name", updated.Name);
            cmd.Parameters.AddWithValue("email", updated.Email);
            cmd.Parameters.AddWithValue("profile_image", (object?)updated.ProfileImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("bio", (object?)updated.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("password_hash", updated.PasswordHash);
            cmd.Parameters.AddWithValue("user_id", id);
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("user_id", id);
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0) return NotFound();

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

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Check if device already exists
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT id FROM user_devices 
                    WHERE user_id = @user_id AND push_token = @push_token
                ";
                checkCmd.Parameters.AddWithValue("user_id", userId);
                checkCmd.Parameters.AddWithValue("push_token", request.PushToken);
                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing device
                    using var updateCmd = connection.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE user_devices 
                        SET device_type = COALESCE(@device_type, device_type),
                            device_name = COALESCE(@device_name, device_name),
                            is_active = true,
                            last_used_at = @last_used_at
                        WHERE user_id = @user_id AND push_token = @push_token
                    ";
                    updateCmd.Parameters.AddWithValue("device_type", (object?)request.DeviceType ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("device_name", (object?)request.DeviceName ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("last_used_at", DateTime.UtcNow);
                    updateCmd.Parameters.AddWithValue("user_id", userId);
                    updateCmd.Parameters.AddWithValue("push_token", request.PushToken);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Create new device
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO user_devices (user_id, push_token, device_type, device_name, is_active, created_at, last_used_at)
                        VALUES (@user_id, @push_token, @device_type, @device_name, true, @created_at, @last_used_at)
                    ";
                    insertCmd.Parameters.AddWithValue("user_id", userId);
                    insertCmd.Parameters.AddWithValue("push_token", request.PushToken);
                    insertCmd.Parameters.AddWithValue("device_type", request.DeviceType ?? "unknown");
                    insertCmd.Parameters.AddWithValue("device_name", (object?)request.DeviceName ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("last_used_at", DateTime.UtcNow);
                    await insertCmd.ExecuteNonQueryAsync();
                }

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

        /// <summary>
        /// Update authenticated user's profile
        /// PUT /api/users/profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UserUpdateDto dto)
        {
            try
            {
                // Get authenticated user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                // Validate input
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                // Check if at least one field is provided
                if (string.IsNullOrWhiteSpace(dto.Name) && 
                    string.IsNullOrWhiteSpace(dto.ProfileImageS3Key) && 
                    string.IsNullOrWhiteSpace(dto.Bio))
                {
                    return BadRequest(new { message = "At least one field (name, profileImageS3Key, or bio) must be provided" });
                }

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Find user
                using var getCmd = connection.CreateCommand();
                getCmd.CommandText = "SELECT user_id, name, email, profile_image, bio, email_confirmed, created_at FROM users WHERE user_id = @user_id";
                getCmd.Parameters.AddWithValue("user_id", userId);
                
                User? user = null;
                using (var reader = await getCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = ReadUserFullFromReader(reader);
                    }
                }
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                string? oldProfileImage = user.ProfileImage;

                // Update fields
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    user.Name = dto.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(dto.ProfileImageS3Key))
                {
                    var s3Key = dto.ProfileImageS3Key.Trim();
                    
                    if (!s3Key.Contains(userId.ToString()))
                    {
                        return BadRequest(new { message = "Invalid profile image S3 key. Must belong to your user account." });
                    }

                    var exists = await _s3Service.FileExistsAsync(s3Key);
                    if (!exists)
                    {
                        return BadRequest(new { message = "Profile image file not found in S3. Please upload the file first." });
                    }

                    user.ProfileImage = s3Key;
                }

                if (dto.Bio != null)
                {
                    user.Bio = dto.Bio.Trim();
                }

                // Update user in database
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET name = @name, profile_image = @profile_image, bio = @bio
                    WHERE user_id = @user_id
                ";
                updateCmd.Parameters.AddWithValue("name", user.Name);
                updateCmd.Parameters.AddWithValue("profile_image", (object?)user.ProfileImage ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("bio", (object?)user.Bio ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("user_id", userId);
                await updateCmd.ExecuteNonQueryAsync();

                // Delete old profile image if changed
                if (!string.IsNullOrWhiteSpace(oldProfileImage) && oldProfileImage != user.ProfileImage)
                {
                    try
                    {
                        await _s3Service.DeleteFileAsync(oldProfileImage);
                        _logger.LogInformation("Deleted old profile image for user {UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old profile image for user {UserId}", userId);
                    }
                }

                _logger.LogInformation("Profile updated for user: {UserId}", userId);

                // Generate download URL for profile image if exists
                string? profileImageUrl = null;
                if (!string.IsNullOrWhiteSpace(user.ProfileImage))
                {
                    try
                    {
                        profileImageUrl = await _s3Service.GenerateDownloadUrlAsync(user.ProfileImage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate download URL for profile image");
                    }
                }

                var response = new UserProfileResponseDto
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    ProfileImageS3Key = user.ProfileImage,
                    ProfileImageUrl = profileImageUrl,
                    Bio = user.Bio,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return StatusCode(500, new { message = "Failed to update profile. Please try again." });
            }
        }

        /// <summary>
        /// Get authenticated user's profile
        /// GET /api/users/profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponseDto>> GetProfile()
        {
            try
            {
                _logger.LogInformation("GetProfile endpoint called");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User ID claim: {UserIdClaim}", userIdClaim ?? "NULL");
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Invalid or missing user ID claim");
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Find user
                _logger.LogInformation("Finding user with ID: {UserId}", userId);
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT user_id, name, email, profile_image, bio, email_confirmed, created_at FROM users WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("user_id", userId);
                
                User? user = null;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = ReadUserFullFromReader(reader);
                    }
                }
                
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return NotFound(new { message = "User not found" });
                }

                // Generate download URL for profile image if exists
                string? profileImageUrl = null;
                if (!string.IsNullOrWhiteSpace(user.ProfileImage))
                {
                    try
                    {
                        profileImageUrl = await _s3Service.GenerateDownloadUrlAsync(user.ProfileImage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate download URL for profile image");
                    }
                }

                var response = new UserProfileResponseDto
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    ProfileImageS3Key = user.ProfileImage,
                    ProfileImageUrl = profileImageUrl,
                    Bio = user.Bio,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile");
                return StatusCode(500, new { message = "Failed to get profile. Please try again." });
            }
        }

        /// <summary>
        /// Get birds owned by a specific user
        /// GET /api/users/{id}/owned-birds
        /// </summary>
        [HttpGet("{id}/owned-birds")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BirdSummaryDto>>> GetOwnedBirds(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
                {
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                if (id != authenticatedUserId)
                {
                    return Forbid();
                }

                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                
                // Check if user exists
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT user_id FROM users WHERE user_id = @user_id";
                checkCmd.Parameters.AddWithValue("user_id", id);
                var userExists = await checkCmd.ExecuteScalarAsync();
                
                if (userExists == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Get birds owned by the user
                var birds = await GetUserBirdsAsync(connection, id);

                _logger.LogInformation("Retrieved {Count} owned birds for user {UserId}", birds.Count, id);

                // Map to DTOs and set ImageUrl (AutoMapper ignores ImageUrl, expects controller to set it)
                var birdDtos = _mapper.Map<List<BirdSummaryDto>>(birds);
                for (int i = 0; i < birdDtos.Count; i++)
                {
                    // ImageUrl was already generated and stored in Bird.ImageUrl
                    // which AutoMapper mapped to ImageS3Key, so we need to copy it
                    birdDtos[i].ImageUrl = birds[i].ImageUrl;
                }

                return Ok(birdDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owned birds for user {UserId}", id);
                return StatusCode(500, new { message = "Failed to retrieve owned birds. Please try again." });
            }
        }

        // Helper methods
        private async Task<User?> GetUserByIdAsync(NpgsqlConnection connection, Guid userId)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT user_id, name, email, profile_image, bio, created_at FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("user_id", userId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUserBasicFromReader(reader);
            }
            return null;
        }

        private async Task<List<Bird>> GetUserBirdsAsync(NpgsqlConnection connection, Guid userId)
        {
            var birds = new List<Bird>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT bird_id, owner_id, name, species, image_url, created_at FROM birds WHERE owner_id = @owner_id";
            cmd.Parameters.AddWithValue("owner_id", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var imageS3Key = reader.IsDBNull(4) ? null : reader.GetString(4);
                string? imageUrl = null;

                // Generate presigned URL for image if S3 key exists
                if (!string.IsNullOrWhiteSpace(imageS3Key))
                {
                    try
                    {
                        imageUrl = await _s3Service.GenerateDownloadUrlAsync(imageS3Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate presigned URL for bird image: {S3Key}", imageS3Key);
                    }
                }

                birds.Add(new Bird
                {
                    BirdId = reader.GetGuid(0),
                    OwnerId = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    Species = reader.GetString(3),
                    ImageUrl = imageUrl ?? imageS3Key,
                    CreatedAt = reader.GetDateTime(5)
                });
            }
            return birds;
        }

        private async Task<List<Story>> GetUserStoriesAsync(NpgsqlConnection connection, Guid userId)
        {
            var stories = new List<Story>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT story_id, author_id, content, mode, created_at FROM stories WHERE author_id = @author_id";
            cmd.Parameters.AddWithValue("author_id", userId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var modeString = reader.GetString(3);
                StoryMode? mode = null;
                if (Enum.TryParse<StoryMode>(modeString, true, out var parsedMode))
                {
                    mode = parsedMode;
                }
                
                stories.Add(new Story
                {
                    StoryId = reader.GetGuid(0),
                    AuthorId = reader.GetGuid(1),
                    Content = reader.GetString(2),
                    Mode = mode,
                    CreatedAt = reader.GetDateTime(4)
                });
            }
            return stories;
        }

        private async Task<List<SupportTransaction>> GetUserSupportTransactionsAsync(NpgsqlConnection connection, Guid userId)
        {
            var transactions = new List<SupportTransaction>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT transaction_id, supporter_id, bird_id, amount, created_at
                FROM support_transactions 
                WHERE supporter_id = @supporter_id
            ";
            cmd.Parameters.AddWithValue("supporter_id", userId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new SupportTransaction
                {
                    TransactionId = reader.GetGuid(0),
                    SupporterId = reader.GetGuid(1),
                    BirdId = reader.GetGuid(2),
                    Amount = reader.GetDecimal(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }
            return transactions;
        }

        private User ReadUserBasicFromReader(NpgsqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                ProfileImage = reader.IsDBNull(3) ? null : reader.GetString(3),
                Bio = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTime(5)
            };
        }

        private User ReadUserFullFromReader(NpgsqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                ProfileImage = reader.IsDBNull(reader.GetOrdinal("profile_image")) ? null : reader.GetString(reader.GetOrdinal("profile_image")),
                Bio = reader.IsDBNull(reader.GetOrdinal("bio")) ? null : reader.GetString(reader.GetOrdinal("bio")),
                EmailConfirmed = reader.GetBoolean(reader.GetOrdinal("email_confirmed")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }

    public class RegisterPushTokenRequest
    {
        public string PushToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }
}
