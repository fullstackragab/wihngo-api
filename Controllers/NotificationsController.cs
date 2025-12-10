namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _db;

        public NotificationsController(INotificationService notificationService, AppDbContext db)
        {
            _notificationService = notificationService;
            _db = db;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        /// <summary>
        /// Get all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.GetUserNotificationsAsync(
                userId.Value, 
                page, 
                pageSize, 
                unreadOnly);

            return Ok(result);
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(new { count });
        }

        /// <summary>
        /// Mark a specific notification as read
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (!success) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<ActionResult<int>> MarkAllAsRead()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(new { markedCount = count });
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var success = await _notificationService.DeleteNotificationAsync(id, userId.Value);
            if (!success) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Get notification preferences
        /// </summary>
        [HttpGet("preferences")]
        public async Task<ActionResult<object>> GetPreferences()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var preferences = await _notificationService.GetPreferencesAsync(userId.Value);
            return Ok(new { preferences });
        }

        /// <summary>
        /// Update notification preference
        /// </summary>
        [HttpPut("preferences")]
        public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreference(
            [FromBody] UpdateNotificationPreferenceDto dto)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var preference = await _notificationService.UpdatePreferenceAsync(userId.Value, dto);
            return Ok(preference);
        }

        /// <summary>
        /// Get notification settings
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<NotificationSettingsDto>> GetSettings()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var settings = await _notificationService.GetSettingsAsync(userId.Value);
            return Ok(settings);
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<NotificationSettingsDto>> UpdateSettings(
            [FromBody] NotificationSettingsDto dto)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var settings = await _notificationService.UpdateSettingsAsync(userId.Value, dto);
            return Ok(settings);
        }

        /// <summary>
        /// Register a device for push notifications
        /// </summary>
        [HttpPost("devices")]
        public async Task<ActionResult<UserDeviceDto>> RegisterDevice([FromBody] RegisterDeviceDto dto)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            // Check if device already exists
            var existingDevice = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.PushToken == dto.PushToken);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.UserId = userId.Value;
                existingDevice.DeviceType = dto.DeviceType;
                existingDevice.DeviceName = dto.DeviceName;
                existingDevice.IsActive = true;
                existingDevice.LastUsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new UserDeviceDto
                {
                    DeviceId = existingDevice.DeviceId,
                    PushToken = existingDevice.PushToken,
                    DeviceType = existingDevice.DeviceType,
                    DeviceName = existingDevice.DeviceName,
                    IsActive = existingDevice.IsActive,
                    LastUsedAt = existingDevice.LastUsedAt,
                    CreatedAt = existingDevice.CreatedAt
                });
            }

            // Create new device
            var device = new UserDevice
            {
                DeviceId = Guid.NewGuid(),
                UserId = userId.Value,
                PushToken = dto.PushToken,
                DeviceType = dto.DeviceType,
                DeviceName = dto.DeviceName,
                IsActive = true,
                LastUsedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserDevices.Add(device);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDevices), new { }, new UserDeviceDto
            {
                DeviceId = device.DeviceId,
                PushToken = device.PushToken,
                DeviceType = device.DeviceType,
                DeviceName = device.DeviceName,
                IsActive = device.IsActive,
                LastUsedAt = device.LastUsedAt,
                CreatedAt = device.CreatedAt
            });
        }

        /// <summary>
        /// Get all devices for current user
        /// </summary>
        [HttpGet("devices")]
        public async Task<ActionResult<object>> GetDevices()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var devices = await _db.UserDevices
                .Where(d => d.UserId == userId.Value)
                .Select(d => new UserDeviceDto
                {
                    DeviceId = d.DeviceId,
                    PushToken = d.PushToken,
                    DeviceType = d.DeviceType,
                    DeviceName = d.DeviceName,
                    IsActive = d.IsActive,
                    LastUsedAt = d.LastUsedAt,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Ok(new { devices });
        }

        /// <summary>
        /// Deactivate a device
        /// </summary>
        [HttpDelete("devices/{deviceId}")]
        public async Task<IActionResult> DeactivateDevice(Guid deviceId)
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var device = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId.Value);

            if (device == null) return NotFound();

            device.IsActive = false;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Test notification (for debugging)
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification()
        {
            var userId = GetUserIdClaim();
            if (!userId.HasValue) return Unauthorized();

            var dto = new CreateNotificationDto
            {
                UserId = userId.Value,
                Type = Models.Enums.NotificationType.BirdLoved,
                Title = "Test Notification",
                Message = "This is a test notification from Wihngo!",
                Priority = Models.Enums.NotificationPriority.Low,
                Channels = Models.Enums.NotificationChannel.InApp | Models.Enums.NotificationChannel.Push
            };

            var notification = await _notificationService.CreateNotificationAsync(dto);
            return Ok(new { notificationId = notification.NotificationId });
        }
    }
}
