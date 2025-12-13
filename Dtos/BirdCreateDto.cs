namespace Wihngo.Dtos
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class BirdCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Species { get; set; }

        [MaxLength(500)]
        public string? Tagline { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// S3 key for bird profile image (e.g., birds/profile-images/{birdId}/{uuid}.jpg)
        /// Use /api/media/upload-url with mediaType='bird-profile-image' to get pre-signed upload URL
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string ImageS3Key { get; set; } = string.Empty;

        /// <summary>
        /// S3 key for bird video (e.g., birds/videos/{birdId}/{uuid}.mp4)
        /// Use /api/media/upload-url with mediaType='bird-video' to get pre-signed upload URL
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string VideoS3Key { get; set; } = string.Empty;
    }
}
