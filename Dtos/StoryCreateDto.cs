using System.ComponentModel.DataAnnotations;
using Wihngo.Models.Enums;

namespace Wihngo.Dtos
{
    /// <summary>
    /// DTO for creating a new story
    /// </summary>
    public class StoryCreateDto
    {
        /// <summary>
        /// Bird ID to tag in this story (required)
        /// </summary>
        [Required]
        public Guid BirdId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional mood/category: LoveAndBond, NewBeginning, ProgressAndWins, FunnyMoment, 
        /// PeacefulMoment, LossAndMemory, CareAndHealth, DailyLife
        /// If not provided, story will have no mood tag.
        /// </summary>
        public StoryMode? Mode { get; set; }

        /// <summary>
        /// S3 key for story image (e.g., users/stories/{userId}/{storyId}/{uuid}.jpg)
        /// Use /api/media/upload-url with mediaType='story-image' to get pre-signed upload URL
        /// Optional - can be null
        /// </summary>
        [MaxLength(1000)]
        public string? ImageS3Key { get; set; }

        /// <summary>
        /// S3 key for story video (e.g., users/stories/{userId}/{storyId}/{uuid}.mp4)
        /// Use /api/media/upload-url with mediaType='story-video' to get pre-signed upload URL
        /// Optional - can be null
        /// </summary>
        [MaxLength(1000)]
        public string? VideoS3Key { get; set; }
    }
}
