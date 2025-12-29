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
    }

    /// <summary>
    /// DTO for updating bird support settings
    /// </summary>
    public class BirdSupportSettingsDto
    {
        /// <summary>
        /// Whether this bird should accept support.
        /// Note: Owner must also have a wallet configured for support to work.
        /// </summary>
        [Required]
        public bool SupportEnabled { get; set; }
    }
}
