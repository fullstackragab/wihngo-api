using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    public class MediaUploadRequestDto
    {
        [Required]
        public string MediaType { get; set; } = string.Empty;

        // Make optional - can be derived from filename or content-type
        public string? FileExtension { get; set; }

        public Guid? RelatedId { get; set; }
        
        // Optional: filename can help derive extension
        public string? FileName { get; set; }
    }
}
