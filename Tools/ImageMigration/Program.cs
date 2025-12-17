using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ImageMigrationTool;

class Program
{
    private static IConfiguration _configuration = null!;
    private static IAmazonS3 _s3Client = null!;
    private static string _bucketName = null!;
    private static string _connectionString = null!;
    private static HttpClient _httpClient = null!;

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Image Migration Tool");
        Console.WriteLine("  Upload images to S3 and update database");
        Console.WriteLine("===========================================\n");

        try
        {
            // Load configuration
            LoadConfiguration();

            // Initialize S3 client
            InitializeS3Client();

            // Initialize HTTP client for downloading images
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            // Run migrations
            await MigrateBirdImages();
            await MigrateStoryImages();

            Console.WriteLine("\n===========================================");
            Console.WriteLine("  Migration completed successfully!");
            Console.WriteLine("===========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static void LoadConfiguration()
    {
        Console.WriteLine("Loading configuration...");

        // Get the directory where the tool is located
        var toolDir = AppContext.BaseDirectory;
        // Navigate up to find the main project (from bin/Debug/net10.0 -> Tools/ImageMigration -> Wihngo)
        var mainProjectDir = Path.GetFullPath(Path.Combine(toolDir, "..", "..", "..", "..", ".."));

        Console.WriteLine($"  Looking for config in: {mainProjectDir}");

        _configuration = new ConfigurationBuilder()
            .SetBasePath(mainProjectDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not found");

        _bucketName = _configuration["AWS:BucketName"]
            ?? Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
            ?? "amzn-s3-wihngo-bucket";

        Console.WriteLine($"  Bucket: {_bucketName}");
        Console.WriteLine($"  Database: Connected");
    }

    static void InitializeS3Client()
    {
        Console.WriteLine("Initializing S3 client...");

        var accessKeyId = _configuration["AWS:AccessKeyId"]
            ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");

        var secretAccessKey = _configuration["AWS:SecretAccessKey"]
            ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        var region = _configuration["AWS:Region"]
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? "us-west-2";

        if (string.IsNullOrWhiteSpace(accessKeyId))
        {
            Console.WriteLine("  WARNING: AWS Access Key ID not found in configuration!");
            Console.WriteLine("  Please add AWS:AccessKeyId to appsettings.Development.json");
            throw new InvalidOperationException("AWS Access Key ID not found");
        }

        if (string.IsNullOrWhiteSpace(secretAccessKey))
        {
            Console.WriteLine("  WARNING: AWS Secret Access Key not found in configuration!");
            Console.WriteLine("  Please add AWS:SecretAccessKey to appsettings.Development.json");
            throw new InvalidOperationException("AWS Secret Access Key not found");
        }

        Console.WriteLine($"  Access Key: {accessKeyId.Substring(0, Math.Min(8, accessKeyId.Length))}...");
        Console.WriteLine($"  Region: {region}");
        Console.WriteLine($"  Bucket: {_bucketName}");

        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region),
            SignatureVersion = "4"
        };

        _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
    }

    static async Task MigrateBirdImages()
    {
        Console.WriteLine("\nFetching birds with external image URLs...\n");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get all birds with external URLs (not S3 keys)
        var birds = await connection.QueryAsync<BirdRecord>(
            @"SELECT bird_id, owner_id, name, image_url
              FROM birds
              WHERE image_url IS NOT NULL
                AND image_url != ''
                AND (image_url LIKE 'http://%' OR image_url LIKE 'https://%')
                AND image_url NOT LIKE '%s3.%amazonaws.com%'
              ORDER BY created_at");

        var birdList = birds.ToList();
        Console.WriteLine($"Found {birdList.Count} birds with external image URLs\n");

        int successCount = 0;
        int failCount = 0;

        foreach (var bird in birdList)
        {
            Console.WriteLine($"Processing: {bird.name} ({bird.bird_id})");
            Console.WriteLine($"  Source: {bird.image_url}");

            try
            {
                // Download image
                var imageData = await DownloadImageAsync(bird.image_url);
                if (imageData == null)
                {
                    Console.WriteLine($"  SKIP: Could not download image");
                    failCount++;
                    continue;
                }

                // Determine file extension from URL or content type
                var extension = GetFileExtension(bird.image_url);

                // Generate S3 key
                var uniqueId = Guid.NewGuid().ToString();
                var s3Key = $"birds/profile-images/{bird.bird_id}/{uniqueId}{extension}";

                // Upload to S3
                await UploadToS3Async(s3Key, imageData, GetContentType(extension));
                Console.WriteLine($"  Uploaded: {s3Key}");

                // Update database with S3 key (not full URL - the app will generate pre-signed URLs)
                await connection.ExecuteAsync(
                    "UPDATE birds SET image_url = @S3Key WHERE bird_id = @BirdId",
                    new { S3Key = s3Key, BirdId = bird.bird_id });

                Console.WriteLine($"  Database updated successfully");
                successCount++;
            }
            catch (AmazonS3Exception s3Ex)
            {
                Console.WriteLine($"  S3 ERROR: {s3Ex.ErrorCode} - {s3Ex.Message}");
                if (s3Ex.ErrorCode == "AccessDenied")
                {
                    Console.WriteLine($"    Check: IAM permissions for bucket '{_bucketName}'");
                }
                failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                failCount++;
            }

            Console.WriteLine();
        }

        Console.WriteLine("===========================================");
        Console.WriteLine($"  Successful: {successCount}");
        Console.WriteLine($"  Failed: {failCount}");
        Console.WriteLine("===========================================");
    }

    static async Task MigrateStoryImages()
    {
        Console.WriteLine("\n\nFetching stories with external image URLs...\n");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get all stories with external URLs (not S3 keys)
        var stories = await connection.QueryAsync<StoryRecord>(
            @"SELECT s.story_id, s.bird_id, s.image_url, b.name as bird_name
              FROM stories s
              JOIN birds b ON s.bird_id = b.bird_id
              WHERE s.image_url IS NOT NULL
                AND s.image_url != ''
                AND (s.image_url LIKE 'http://%' OR s.image_url LIKE 'https://%')
                AND s.image_url NOT LIKE '%s3.%amazonaws.com%'
                AND s.image_url NOT LIKE 'stories/%'
              ORDER BY s.created_at");

        var storyList = stories.ToList();
        Console.WriteLine($"Found {storyList.Count} stories with external image URLs\n");

        int successCount = 0;
        int failCount = 0;

        foreach (var story in storyList)
        {
            Console.WriteLine($"Processing story for: {story.bird_name} ({story.story_id})");
            Console.WriteLine($"  Source: {story.image_url}");

            try
            {
                // Download image
                var imageData = await DownloadImageAsync(story.image_url);
                if (imageData == null)
                {
                    Console.WriteLine($"  SKIP: Could not download image");
                    failCount++;
                    continue;
                }

                // Determine file extension from URL or content type
                var extension = GetFileExtension(story.image_url);

                // Generate S3 key
                var uniqueId = Guid.NewGuid().ToString();
                var s3Key = $"stories/{story.bird_id}/{uniqueId}{extension}";

                // Upload to S3
                await UploadToS3Async(s3Key, imageData, GetContentType(extension));
                Console.WriteLine($"  Uploaded: {s3Key}");

                // Update database with S3 key
                await connection.ExecuteAsync(
                    "UPDATE stories SET image_url = @S3Key WHERE story_id = @StoryId",
                    new { S3Key = s3Key, StoryId = story.story_id });

                Console.WriteLine($"  Database updated successfully");
                successCount++;
            }
            catch (AmazonS3Exception s3Ex)
            {
                Console.WriteLine($"  S3 ERROR: {s3Ex.ErrorCode} - {s3Ex.Message}");
                if (s3Ex.ErrorCode == "AccessDenied")
                {
                    Console.WriteLine($"    Check: IAM permissions for bucket '{_bucketName}'");
                }
                failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                failCount++;
            }

            Console.WriteLine();
        }

        Console.WriteLine("===========================================");
        Console.WriteLine($"  Stories - Successful: {successCount}");
        Console.WriteLine($"  Stories - Failed: {failCount}");
        Console.WriteLine("===========================================");
    }

    static async Task<byte[]?> DownloadImageAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            Console.WriteLine($"  Download failed: HTTP {(int)response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Download error: {ex.Message}");
            return null;
        }
    }

    static async Task UploadToS3Async(string s3Key, byte[] data, string contentType)
    {
        using var stream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            InputStream = stream,
            ContentType = contentType,
            // Make the object publicly readable (optional - remove if using pre-signed URLs only)
            // CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request);
    }

    static string GetFileExtension(string url)
    {
        // Try to get extension from URL
        var uri = new Uri(url);
        var path = uri.AbsolutePath;

        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            return ".jpg";
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return ".png";
        if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            return ".gif";
        if (path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            return ".webp";

        // Default to jpg
        return ".jpg";
    }

    static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

class BirdRecord
{
    // Using snake_case to match PostgreSQL column names
    public Guid bird_id { get; set; }
    public Guid owner_id { get; set; }
    public string name { get; set; } = string.Empty;
    public string image_url { get; set; } = string.Empty;
}

class StoryRecord
{
    // Using snake_case to match PostgreSQL column names
    public Guid story_id { get; set; }
    public Guid bird_id { get; set; }
    public string image_url { get; set; } = string.Empty;
    public string bird_name { get; set; } = string.Empty;
}
