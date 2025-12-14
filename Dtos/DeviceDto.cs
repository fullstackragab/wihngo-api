namespace Wihngo.Dtos
{
    using System;

    public class RegisterDeviceDto
    {
        public string PushToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }

    public class UnregisterDeviceDto
    {
        public string DeviceToken { get; set; } = string.Empty;
    }

    public class RegisterDonationDeviceDto
    {
        public string ExpoPushToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "ios" or "android"
    }

    public class UserDeviceDto
    {
        public Guid DeviceId { get; set; }
        public string PushToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
