using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IS3Service _s3Service;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IS3Service s3Service, ILogger<MediaController> logger)
        {
            _s3Service = s3Service;
            _logger = logger;
        }

        /// <summary>
        /// DIAGNOSTIC ENDPOINT - Test what the API receives
        /// </summary>
        [HttpPost("test-request")]
        public IActionResult TestRequest([FromBody] object rawRequest)
        {
            _logger.LogInformation("?? RAW REQUEST RECEIVED: {Request}", System.Text.Json.JsonSerializer.Serialize(rawRequest));
            
            return Ok(new 
            { 
                message = "Request received successfully",
                received = rawRequest,
                type = rawRequest?.GetType().Name
            });
        }

        /// <summary>
        /// Generate a pre-signed URL for uploading media to S3
        /// </summary>
        /// <remarks>
        /// Valid media types:
        /// - profile-image: User profile image
        /// - story-image: Story image (optional relatedId as storyId)
        /// - story-video: Story video
        /// - bird-profile-image: Bird profile image (optional relatedId as birdId)
        /// - bird-video: Bird video (optional relatedId as birdId)
        ///
        /// Valid file extensions: .jpg, .jpeg, .png, .gif, .webp, .mp4, .mov, .avi, .webm
        /// </remarks>
        [HttpPost("upload-url")]
        public async Task<ActionResult<MediaUploadResponseDto>> GenerateUploadUrl([FromBody] MediaUploadRequestDto request)
        {
            try
            {
                // Log the incoming request for debugging
                _logger.LogInformation("?? Upload URL request received - MediaType: {MediaType}, FileExtension: {FileExtension}, FileName: {FileName}, RelatedId: {RelatedId}",
                    request?.MediaType ?? "NULL",
                    request?.FileExtension ?? "NULL",
                    request?.FileName ?? "NULL",
                    request?.RelatedId?.ToString() ?? "NULL");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    
                    _logger.LogWarning("? Model validation failed: {Errors}", 
                        string.Join(", ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value ?? Array.Empty<string>())}")));
                    
                    return BadRequest(new 
                    { 
                        message = "Validation failed", 
                        errors = errors,
                        received = new 
                        {
                            mediaType = request?.MediaType,
                            fileExtension = request?.FileExtension,
                            fileName = request?.FileName,
                            relatedId = request?.RelatedId
                        },
                        hint = "Expected JSON format: { \"mediaType\": \"bird-profile-image\", \"fileExtension\": \"png\" }"
                    });
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // Validate media type
                var validMediaTypes = new[] { "profile-image", "story-image", "story-video", "story-audio", "bird-profile-image", "bird-video" };
                if (!validMediaTypes.Contains(request.MediaType.ToLower()))
                {
                    _logger.LogWarning("? Invalid media type: {MediaType}", request.MediaType);
                    return BadRequest(new { message = "Invalid media type. Valid types: " + string.Join(", ", validMediaTypes) });
                }

                // Determine file extension
                string? extension = request.FileExtension;
                
                // If no extension provided, try to derive from filename
                if (string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(request.FileName))
                {
                    extension = Path.GetExtension(request.FileName);
                    _logger.LogInformation("?? Derived extension from filename: {Extension}", extension);
                }
                
                // If still no extension, default based on media type
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = request.MediaType.ToLower().Contains("video") ? ".mp4"
                              : request.MediaType.ToLower().Contains("audio") ? ".m4a"
                              : ".jpg";
                    _logger.LogInformation("?? Using default extension: {Extension}", extension);
                }

                // Normalize file extension - ensure it starts with a dot and is lowercase
                extension = extension.ToLower();
                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }

                // Validate file extension
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".avi", ".webm", ".m4a", ".mp3", ".wav", ".aac", ".ogg" };
                if (!validExtensions.Contains(extension))
                {
                    _logger.LogWarning("? Invalid file extension: {Extension}", extension);
                    return BadRequest(new { message = "Invalid file extension. Valid extensions: " + string.Join(", ", validExtensions) });
                }

                // Validate relatedId for media types that require it
                // Note: All media types now support optional RelatedId (can upload before creating the related entity)
                // This allows mobile apps to upload media first, get S3 keys, then create stories/birds with those keys

                var (uploadUrl, s3Key) = await _s3Service.GenerateUploadUrlAsync(
                    userId,
                    request.MediaType,
                    extension,
                    request.RelatedId);

                _logger.LogInformation("? Generated upload URL for user {UserId}, media type {MediaType}, S3 key: {S3Key}", 
                    userId, request.MediaType, s3Key);

                // Determine recommended Content-Type
                var contentType = extension.ToLower() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".mp4" => "video/mp4",
                    ".mov" => "video/quicktime",
                    ".avi" => "video/x-msvideo",
                    ".webm" => "video/webm",
                    ".m4a" => "audio/mp4",
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".aac" => "audio/aac",
                    ".ogg" => "audio/ogg",
                    _ => "application/octet-stream"
                };

                return Ok(new MediaUploadResponseDto
                {
                    UploadUrl = uploadUrl,
                    S3Key = s3Key,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Instructions = $"Use PUT request to upload. Set Content-Type header to: {contentType}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error generating upload URL");
                return StatusCode(500, new { message = "Failed to generate upload URL", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate a pre-signed URL for downloading/viewing media from S3
        /// </summary>
        [HttpPost("download-url")]
        public async Task<ActionResult<MediaDownloadResponseDto>> GenerateDownloadUrl([FromBody] MediaDownloadRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                if (string.IsNullOrWhiteSpace(request.S3Key))
                {
                    return BadRequest(new { message = "S3Key is required" });
                }

                var downloadUrl = await _s3Service.GenerateDownloadUrlAsync(request.S3Key);

                _logger.LogInformation("Generated download URL for S3 key {S3Key}", request.S3Key);

                return Ok(new MediaDownloadResponseDto
                {
                    DownloadUrl = downloadUrl,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download URL for S3 key {S3Key}", request.S3Key);
                return StatusCode(500, new { message = "Failed to generate download URL" });
            }
        }

        /// <summary>
        /// Delete a media file from S3
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteMedia([FromQuery] string s3Key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s3Key))
                {
                    return BadRequest(new { message = "S3Key is required" });
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // Basic security check: ensure the s3Key belongs to the user
                if (!s3Key.Contains(userId.ToString()))
                {
                    _logger.LogWarning("User {UserId} attempted to delete file {S3Key} that doesn't belong to them", 
                        userId, s3Key);
                    return Forbid();
                }

                // Check if file exists
                var exists = await _s3Service.FileExistsAsync(s3Key);
                if (!exists)
                {
                    return NotFound(new { message = "File not found" });
                }

                await _s3Service.DeleteFileAsync(s3Key);

                _logger.LogInformation("User {UserId} deleted file {S3Key}", userId, s3Key);

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {S3Key}", s3Key);
                return StatusCode(500, new { message = "Failed to delete file" });
            }
        }

        /// <summary>
        /// Check if a media file exists in S3
        /// </summary>
        [HttpGet("exists")]
        public async Task<ActionResult<bool>> CheckFileExists([FromQuery] string s3Key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s3Key))
                {
                    return BadRequest(new { message = "S3Key is required" });
                }

                var exists = await _s3Service.FileExistsAsync(s3Key);

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists for S3 key {S3Key}", s3Key);
                return StatusCode(500, new { message = "Failed to check file existence" });
            }
        }
    }
}
