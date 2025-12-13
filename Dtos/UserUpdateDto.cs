namespace Wihngo.Dtos
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// DTO for updating user profile information
    /// </summary>
    public class UserUpdateDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>
        /// S3 key for profile image (e.g., users/profile-images/{userId}/{uuid}.jpg)
        /// Use /api/media/upload-url to get pre-signed upload URL first
        /// </summary>
        [MaxLength(1000)]
        public string? ProfileImageS3Key { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }
    }
}
