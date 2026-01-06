namespace Wihngo.Configuration
{
    public class AwsConfiguration
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = "amzn-s3-wihngo-bucket";
        public string Region { get; set; } = "us-east-1";

        // Pre-signed URL expiration time in minutes
        public int PresignedUrlExpirationMinutes { get; set; } = 10;
    }

    public class AwsPublicBucketConfiguration
    {
        public string Bucket { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
    }
}
