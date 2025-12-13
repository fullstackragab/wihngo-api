using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    /// <summary>
    /// DTO for updating an existing story
    /// </summary>
    public class StoryUpdateDto
    {
        [MaxLength(5000)]
        public string? Content { get; set; }

        /// <summary>
        /// S3 key for story image (e.g., users/stories/{userId}/{storyId}/{uuid}.jpg)
        /// Use /api/media/upload-url with mediaType='story-image' to get pre-signed upload URL
        /// Set to empty string to remove the image
        /// </summary>
        [MaxLength(1000)]
        public string? ImageS3Key { get; set; }
    }
}
