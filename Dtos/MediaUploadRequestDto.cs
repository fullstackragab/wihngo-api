using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    public class MediaUploadRequestDto
    {
        [Required]
        public string MediaType { get; set; } = string.Empty;

        [Required]
        public string FileExtension { get; set; } = string.Empty;

        public Guid? RelatedId { get; set; }
    }
}
