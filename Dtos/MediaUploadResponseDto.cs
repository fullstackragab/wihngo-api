namespace Wihngo.Dtos
{
    public class MediaUploadResponseDto
    {
        public string UploadUrl { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Instructions { get; set; } = "Use PUT request to upload the file to the uploadUrl";
    }
}
