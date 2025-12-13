namespace Wihngo.Dtos
{
    public class MediaDownloadResponseDto
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
