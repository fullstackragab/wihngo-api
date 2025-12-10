namespace Wihngo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class UserDevice
    {
        [Key]
        public Guid DeviceId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(500)]
        public string PushToken { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DeviceType { get; set; } // "ios", "android"

        [MaxLength(200)]
        public string? DeviceName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
