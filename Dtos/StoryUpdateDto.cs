using System.ComponentModel.DataAnnotations;
using Wihngo.Models.Enums;

namespace Wihngo.Dtos
{
    /// <summary>
    /// DTO for updating an existing story
    /// </summary>
    public class StoryUpdateDto
    {
        /// <summary>
        /// List of bird IDs to tag in this story (minimum 1 required if provided)
        /// </summary>
        [MinLength(1, ErrorMessage = "At least one bird must be selected")]
        public List<Guid>? BirdIds { get; set; }

        [MaxLength(5000)]
        public string? Content { get; set; }

        /// <summary>
        /// Story mode/category: LoveAndBond, NewBeginning, ProgressAndWins, FunnyMoment, 
        /// PeacefulMoment, LossAndMemory, CareAndHealth, DailyLife
        /// </summary>
        public StoryMode? Mode { get; set; }

        /// <summary>
        /// S3 key for story image (e.g., users/stories/{userId}/{storyId}/{uuid}.jpg)
        /// Use /api/media/upload-url with mediaType='story-image' to get pre-signed upload URL
        /// Set to empty string to remove the image
        /// </summary>
        [MaxLength(1000)]
        public string? ImageS3Key { get; set; }

        /// <summary>
        /// S3 key for story video (e.g., users/stories/{userId}/{storyId}/{uuid}.mp4)
        /// Use /api/media/upload-url with mediaType='story-video' to get pre-signed upload URL
        /// Set to empty string to remove the video
        /// </summary>
        [MaxLength(1000)]
        public string? VideoS3Key { get; set; }
    }
}
