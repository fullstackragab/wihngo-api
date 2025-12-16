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
        /// Bird ID to tag in this story (optional - if not provided, bird won't be changed)
        /// </summary>
        public Guid? BirdId { get; set; }

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

        /// <summary>
        /// S3 key for story audio (e.g., users/stories/{userId}/{storyId}/{uuid}.m4a)
        /// Use /api/media/upload-url with mediaType='story-audio' to get pre-signed upload URL
        /// Set to empty string to remove the audio
        /// </summary>
        [MaxLength(1000)]
        public string? AudioS3Key { get; set; }
    }
}
