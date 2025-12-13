using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsConfiguration _config;
        private readonly ILogger<S3Service> _logger;

        public S3Service(
            IOptions<AwsConfiguration> config,
            ILogger<S3Service> logger)
        {
            _config = config.Value;
            _logger = logger;

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_config.AccessKeyId) || 
                string.IsNullOrWhiteSpace(_config.SecretAccessKey))
            {
                throw new InvalidOperationException(
                    "AWS credentials are not configured. Please set AWS:AccessKeyId and AWS:SecretAccessKey in appsettings.json or environment variables.");
            }

            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_config.Region),
                SignatureVersion = "4" // Use Signature Version 4
            };

            _s3Client = new AmazonS3Client(
                _config.AccessKeyId,
                _config.SecretAccessKey,
                s3Config);

            _logger.LogInformation("S3Service initialized for bucket {BucketName} in region {Region}", 
                _config.BucketName, _config.Region);
        }

        public async Task<(string uploadUrl, string s3Key)> GenerateUploadUrlAsync(
            Guid userId,
            string mediaType,
            string fileExtension,
            Guid? relatedId = null)
        {
            try
            {
                // Generate unique filename
                var uniqueId = Guid.NewGuid().ToString();
                
                // Ensure extension starts with dot
                if (!fileExtension.StartsWith("."))
                {
                    fileExtension = "." + fileExtension;
                }

                // Build S3 key based on media type and folder structure
                var s3Key = BuildS3Key(userId, mediaType, relatedId, uniqueId, fileExtension);

                // Get the content type for this file extension
                var contentType = GetContentType(fileExtension);

                // Generate pre-signed URL for upload
                // IMPORTANT: ContentType MUST match what the mobile app will send
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _config.BucketName,
                    Key = s3Key,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(_config.PresignedUrlExpirationMinutes),
                    ContentType = contentType // This must match the Content-Type header sent by mobile app
                };

                var uploadUrl = await _s3Client.GetPreSignedURLAsync(request);

                _logger.LogInformation("Generated upload URL for user {UserId}, type {MediaType}, key {S3Key}, contentType {ContentType}", 
                    userId, mediaType, s3Key, contentType);

                return (uploadUrl, s3Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating upload URL for user {UserId}, type {MediaType}", 
                    userId, mediaType);
                throw;
            }
        }

        public async Task<string> GenerateDownloadUrlAsync(string s3Key)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _config.BucketName,
                    Key = s3Key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddMinutes(_config.PresignedUrlExpirationMinutes)
                };

                var downloadUrl = await _s3Client.GetPreSignedURLAsync(request);

                _logger.LogInformation("Generated download URL for key {S3Key}", s3Key);

                return downloadUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download URL for key {S3Key}", s3Key);
                throw;
            }
        }

        /// <summary>
        /// Generate download URLs for multiple S3 keys in bulk (more efficient than one-by-one)
        /// </summary>
        /// <param name="s3Keys">List of S3 keys to generate URLs for</param>
        /// <returns>Dictionary mapping S3 key to download URL</returns>
        public async Task<Dictionary<string, string>> GenerateBulkDownloadUrlsAsync(IEnumerable<string> s3Keys)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var s3Key in s3Keys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                try
                {
                    var url = await GenerateDownloadUrlAsync(s3Key);
                    result[s3Key] = url;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for key {S3Key}", s3Key);
                    // Continue with other keys
                }
            }

            return result;
        }

        public async Task DeleteFileAsync(string s3Key)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = s3Key
                };

                await _s3Client.DeleteObjectAsync(request);

                _logger.LogInformation("Deleted file with key {S3Key}", s3Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file with key {S3Key}", s3Key);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string s3Key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _config.BucketName,
                    Key = s3Key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists for key {S3Key}", s3Key);
                throw;
            }
        }

        private string BuildS3Key(Guid userId, string mediaType, Guid? relatedId, string uniqueId, string extension)
        {
            return mediaType.ToLower() switch
            {
                "profile-image" => $"users/profile-images/{userId}/{uniqueId}{extension}",
                "story-image" => $"users/stories/{userId}/{relatedId}/{uniqueId}{extension}",
                "story-video" => $"users/videos/{userId}/{uniqueId}{extension}",
                "bird-profile-image" => $"birds/profile-images/{relatedId}/{uniqueId}{extension}",
                "bird-video" => $"birds/videos/{relatedId}/{uniqueId}{extension}",
                _ => throw new ArgumentException($"Invalid media type: {mediaType}", nameof(mediaType))
            };
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
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
        }
    }
}
