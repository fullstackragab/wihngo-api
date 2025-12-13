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
        /// Generate a pre-signed URL for uploading media to S3
        /// </summary>
        /// <remarks>
        /// Valid media types:
        /// - profile-image: User profile image
        /// - story-image: Story image (requires relatedId as storyId)
        /// - story-video: Story video
        /// - bird-profile-image: Bird profile image (requires relatedId as birdId)
        /// - bird-video: Bird video (requires relatedId as birdId)
        /// 
        /// Valid file extensions: .jpg, .jpeg, .png, .gif, .webp, .mp4, .mov, .avi, .webm
        /// </remarks>
        [HttpPost("upload-url")]
        public async Task<ActionResult<MediaUploadResponseDto>> GenerateUploadUrl([FromBody] MediaUploadRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = ModelState });
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // Validate media type
                var validMediaTypes = new[] { "profile-image", "story-image", "story-video", "bird-profile-image", "bird-video" };
                if (!validMediaTypes.Contains(request.MediaType.ToLower()))
                {
                    return BadRequest(new { message = "Invalid media type. Valid types: " + string.Join(", ", validMediaTypes) });
                }

                // Validate file extension
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".avi", ".webm" };
                var extension = request.FileExtension.ToLower();
                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }
                if (!validExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Invalid file extension. Valid extensions: " + string.Join(", ", validExtensions) });
                }

                // Validate relatedId for media types that require it
                if ((request.MediaType.ToLower() == "story-image" || 
                     request.MediaType.ToLower() == "bird-profile-image" || 
                     request.MediaType.ToLower() == "bird-video") && 
                    !request.RelatedId.HasValue)
                {
                    return BadRequest(new { message = $"RelatedId is required for media type: {request.MediaType}" });
                }

                var (uploadUrl, s3Key) = await _s3Service.GenerateUploadUrlAsync(
                    userId,
                    request.MediaType,
                    extension,
                    request.RelatedId);

                _logger.LogInformation("Generated upload URL for user {UserId}, media type {MediaType}", 
                    userId, request.MediaType);

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
                _logger.LogError(ex, "Error generating upload URL");
                return StatusCode(500, new { message = "Failed to generate upload URL" });
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
