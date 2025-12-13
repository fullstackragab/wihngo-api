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

        [MaxLength(1000)]
        public string? ProfileImage { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }
    }
}
