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

    /// <summary>
    /// DTO for updating a bird's profile
    /// </summary>
    public class BirdUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Species { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Age { get; set; }

        /// <summary>
        /// S3 key for bird profile image. If provided, updates the bird's image.
        /// Use POST /api/birds/{id}/image to upload and get this key.
        /// </summary>
        [MaxLength(1000)]
        public string? ImageS3Key { get; set; }
    }

    /// <summary>
    /// Response from bird image upload
    /// </summary>
    public class BirdImageUploadResponse
    {
        /// <summary>
        /// S3 key for the uploaded image (use this to update bird profile)
        /// </summary>
        public string S3Key { get; set; } = string.Empty;

        /// <summary>
        /// Full URL for immediate display
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
