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
        /// S3 key for profile image (e.g., users/{userId}/profile-image-{timestamp}.jpg)
        /// Use POST /api/users/me/profile-image to upload and get this key
        /// </summary>
        [MaxLength(1000)]
        public string? ProfileImageS3Key { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }
    }

    /// <summary>
    /// Response from profile image upload
    /// </summary>
    public class ProfileImageUploadResponse
    {
        /// <summary>
        /// S3 key for the uploaded image (use this to update profile)
        /// </summary>
        public string S3Key { get; set; } = string.Empty;

        /// <summary>
        /// Full URL for immediate display
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
