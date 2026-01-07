using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonS3 _publicS3Client;
        private readonly AwsConfiguration _config;
        private readonly AwsPublicBucketConfiguration _publicConfig;
        private readonly ILogger<S3Service> _logger;
        private readonly IMemoryCache _urlCache;

        // Media types that use the public bucket
        private static readonly HashSet<string> PublicMediaTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "bird-profile-image",
            "bird-video",
            "love-video-image",
            "love-video-video"
        };

        public S3Service(
            IOptions<AwsConfiguration> config,
            IOptions<AwsPublicBucketConfiguration> publicConfig,
            ILogger<S3Service> logger,
            IMemoryCache memoryCache)
        {
            _config = config.Value;
            _publicConfig = publicConfig.Value;
            _logger = logger;
            _urlCache = memoryCache;

            // Validate private bucket configuration
            if (string.IsNullOrWhiteSpace(_config.AccessKeyId) ||
                string.IsNullOrWhiteSpace(_config.SecretAccessKey))
            {
                throw new InvalidOperationException(
                    "AWS credentials are not configured. Please set AWS:AccessKeyId and AWS:SecretAccessKey in appsettings.json or environment variables.");
            }

            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_config.Region),
                SignatureVersion = "4"
            };

            _s3Client = new AmazonS3Client(
                _config.AccessKeyId,
                _config.SecretAccessKey,
                s3Config);

            // Initialize public bucket client if configured
            if (!string.IsNullOrWhiteSpace(_publicConfig.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(_publicConfig.SecretAccessKey) &&
                !string.IsNullOrWhiteSpace(_publicConfig.Bucket))
            {
                _publicS3Client = new AmazonS3Client(
                    _publicConfig.AccessKeyId,
                    _publicConfig.SecretAccessKey,
                    s3Config);

                _logger.LogInformation("S3Service initialized for buckets {PrivateBucket} (private) and {PublicBucket} (public) in region {Region}",
                    _config.BucketName, _publicConfig.Bucket, _config.Region);
            }
            else
            {
                _publicS3Client = _s3Client; // Fallback to private client
                _logger.LogWarning("Public bucket not configured, using private bucket for all uploads");
            }
        }

        private bool IsPublicMediaType(string mediaType) => PublicMediaTypes.Contains(mediaType);

        private string GetBucketForMediaType(string mediaType) =>
            IsPublicMediaType(mediaType) && !string.IsNullOrWhiteSpace(_publicConfig.Bucket)
                ? _publicConfig.Bucket
                : _config.BucketName;

        private IAmazonS3 GetClientForMediaType(string mediaType) =>
            IsPublicMediaType(mediaType) ? _publicS3Client : _s3Client;

        private bool IsPublicBucketKey(string s3Key) =>
            s3Key.StartsWith("birds/", StringComparison.OrdinalIgnoreCase) ||
            s3Key.StartsWith("love-videos/", StringComparison.OrdinalIgnoreCase);

        private string GetBucketForKey(string s3Key) =>
            IsPublicBucketKey(s3Key) && !string.IsNullOrWhiteSpace(_publicConfig.Bucket)
                ? _publicConfig.Bucket
                : _config.BucketName;

        private IAmazonS3 GetClientForKey(string s3Key) =>
            IsPublicBucketKey(s3Key) ? _publicS3Client : _s3Client;

        private string GetPublicUrl(string s3Key) =>
            $"https://{_publicConfig.Bucket}.s3.{_config.Region}.amazonaws.com/{s3Key}";

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

                // Use public bucket for bird media, private bucket for other media
                var bucketName = GetBucketForMediaType(mediaType);

                // Generate pre-signed URL for upload
                // IMPORTANT: ContentType MUST match what the mobile app will send
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = s3Key,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(_config.PresignedUrlExpirationMinutes),
                    ContentType = contentType // This must match the Content-Type header sent by mobile app
                };

                var client = GetClientForMediaType(mediaType);
                var uploadUrl = await client.GetPreSignedURLAsync(request);

                _logger.LogInformation("Generated upload URL for user {UserId}, type {MediaType}, bucket {Bucket}, key {S3Key}, contentType {ContentType}",
                    userId, mediaType, bucketName, s3Key, contentType);

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
            // For public bucket keys (bird images), return direct public URL
            if (IsPublicBucketKey(s3Key))
            {
                return GetPublicUrl(s3Key);
            }

            // Use cache to return the same URL for repeated requests
            // This enables client-side image caching since the URL stays consistent
            var cacheKey = $"s3_url:{s3Key}";

            if (_urlCache.TryGetValue(cacheKey, out string? cachedUrl) && !string.IsNullOrEmpty(cachedUrl))
            {
                return cachedUrl;
            }

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

                // Cache URL for half the expiration time to ensure URLs are still valid when used
                var cacheExpiration = TimeSpan.FromMinutes(_config.PresignedUrlExpirationMinutes / 2);
                _urlCache.Set(cacheKey, downloadUrl, cacheExpiration);

                _logger.LogDebug("Generated and cached download URL for key {S3Key}", s3Key);

                return downloadUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download URL for key {S3Key}", s3Key);
                throw;
            }
        }

        /// <summary>
        /// Generate download URLs for multiple S3 keys in bulk using PARALLEL processing
        /// </summary>
        /// <param name="s3Keys">List of S3 keys to generate URLs for</param>
        /// <returns>Dictionary mapping S3 key to download URL</returns>
        public async Task<Dictionary<string, string>> GenerateBulkDownloadUrlsAsync(IEnumerable<string> s3Keys)
        {
            var validKeys = s3Keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();

            // Generate all URLs in PARALLEL for performance
            var urlTasks = validKeys.Select(async key =>
            {
                try
                {
                    var url = await GenerateDownloadUrlAsync(key);
                    return (key, url, success: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate download URL for key {S3Key}", key);
                    return (key, url: (string?)null, success: false);
                }
            });

            var results = await Task.WhenAll(urlTasks);

            return results
                .Where(r => r.success && r.url != null)
                .ToDictionary(r => r.key, r => r.url!);
        }

        public async Task DeleteFileAsync(string s3Key)
        {
            try
            {
                var bucketName = GetBucketForKey(s3Key);
                var client = GetClientForKey(s3Key);
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = s3Key
                };

                await client.DeleteObjectAsync(request);

                _logger.LogInformation("Deleted file from bucket {Bucket} with key {S3Key}", bucketName, s3Key);
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
                var bucketName = GetBucketForKey(s3Key);
                var client = GetClientForKey(s3Key);
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = s3Key
                };

                await client.GetObjectMetadataAsync(request);
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

        public async Task UploadFileAsync(string s3Key, Stream stream, string contentType)
        {
            try
            {
                var bucketName = GetBucketForKey(s3Key);
                var client = GetClientForKey(s3Key);
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = s3Key,
                    InputStream = stream,
                    ContentType = contentType
                };

                await client.PutObjectAsync(request);

                _logger.LogInformation("Uploaded file to bucket {Bucket} with key {S3Key}", bucketName, s3Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to S3 with key {S3Key}", s3Key);
                throw;
            }
        }

        private string BuildS3Key(Guid userId, string mediaType, Guid? relatedId, string uniqueId, string extension)
        {
            return mediaType.ToLower() switch
            {
                "profile-image" => $"users/profile-images/{userId}/{uniqueId}{extension}",
                // For story media, use relatedId (storyId) if provided, otherwise just userId
                "story-image" => relatedId.HasValue
                    ? $"users/stories/{userId}/{relatedId}/{uniqueId}{extension}"
                    : $"users/stories/{userId}/{uniqueId}{extension}",
                "story-video" => relatedId.HasValue
                    ? $"users/videos/{userId}/{relatedId}/{uniqueId}{extension}"
                    : $"users/videos/{userId}/{uniqueId}{extension}",
                "story-audio" => relatedId.HasValue
                    ? $"users/stories/{userId}/{relatedId}/{uniqueId}{extension}"
                    : $"users/stories/{userId}/{uniqueId}{extension}",
                // For bird media without relatedId (before bird creation), use userId as folder
                "bird-profile-image" => $"birds/profile-images/{relatedId ?? userId}/{uniqueId}{extension}",
                "bird-video" => $"birds/videos/{relatedId ?? userId}/{uniqueId}{extension}",
                // Love videos use public bucket, organized by user
                "love-video-image" => $"love-videos/images/{userId}/{uniqueId}{extension}",
                "love-video-video" => $"love-videos/videos/{userId}/{uniqueId}{extension}",
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
                ".m4a" => "audio/mp4",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".aac" => "audio/aac",
                ".ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };
        }
    }
}
