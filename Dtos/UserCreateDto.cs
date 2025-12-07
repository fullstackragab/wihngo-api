namespace Wihngo.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class UserCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ProfileImage { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }
    }
}
