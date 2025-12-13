namespace Wihngo.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class ChangePasswordDto
    {
        [Required]
        [MinLength(6)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
