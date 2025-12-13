using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    public class MediaDownloadRequestDto
    {
        [Required]
        public string S3Key { get; set; } = string.Empty;
    }
}
