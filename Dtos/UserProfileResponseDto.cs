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
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
