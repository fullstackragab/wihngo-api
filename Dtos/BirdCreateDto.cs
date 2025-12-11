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

        [Required]
        [MaxLength(1000)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string VideoUrl { get; set; } = string.Empty;
    }
}
