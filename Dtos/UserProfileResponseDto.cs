namespace Wihngo.Dtos
{
    using System;

    /// <summary>
    /// DTO for user profile response after update
    /// </summary>
    public class UserProfileResponseDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// S3 key for profile image storage
        /// </summary>
        public string? ProfileImageS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for profile image (expires in 10 minutes)
        /// </summary>
        public string? ProfileImageUrl { get; set; }
        
        public string? Bio { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
