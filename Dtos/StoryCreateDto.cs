using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    /// <summary>
    /// DTO for creating a new story
    /// </summary>
    public class StoryCreateDto
    {
        [Required]
        public Guid BirdId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// S3 key for story image (e.g., users/stories/{userId}/{storyId}/{uuid}.jpg)
        /// Use /api/media/upload-url with mediaType='story-image' to get pre-signed upload URL
        /// Optional - can be null
        /// </summary>
        [MaxLength(1000)]
        public string? ImageS3Key { get; set; }
    }
}
