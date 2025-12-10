namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PushNotificationService> _logger;
        private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

        public PushNotificationService(
            AppDbContext db,
            HttpClient httpClient,
            ILogger<PushNotificationService> logger)
        {
            _db = db;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendPushNotificationAsync(Notification notification)
        {
            try
            {
                // Get all active devices for the user
                var devices = await _db.UserDevices
                    .Where(d => d.UserId == notification.UserId && d.IsActive)
                    .ToListAsync();

                if (!devices.Any())
                {
                    _logger.LogInformation($"No active devices found for user {notification.UserId}");
                    return;
                }

                // Prepare notification data
                var data = new Dictionary<string, object>
                {
                    { "notificationId", notification.NotificationId },
                    { "type", notification.Type.ToString() }
                };

                if (!string.IsNullOrEmpty(notification.DeepLink))
                    data["deepLink"] = notification.DeepLink;

                if (notification.BirdId.HasValue)
                    data["birdId"] = notification.BirdId.Value;

                if (notification.StoryId.HasValue)
                    data["storyId"] = notification.StoryId.Value;

                // Send to all devices
                var tasks = devices.Select(device => 
                    SendToDeviceAsync(device.PushToken, notification.Title, notification.Message, data));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending push notification for notification {notification.NotificationId}");
            }
        }

        public async Task SendToDeviceAsync(string pushToken, string title, string message, object? data = null)
        {
            if (!IsValidPushToken(pushToken))
            {
                _logger.LogWarning($"Invalid push token format: {pushToken}");
                return;
            }

            try
            {
                var payload = new
                {
                    to = pushToken,
                    title = title,
                    body = message,
                    data = data ?? new { },
                    sound = "default",
                    priority = "high",
                    channelId = "default"
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ExpoApiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Expo push notification failed: {response.StatusCode}, {responseBody}");
                    
                    // Check if token is invalid and deactivate device
                    if (responseBody.Contains("DeviceNotRegistered") || responseBody.Contains("InvalidCredentials"))
                    {
                        await DeactivateDeviceByTokenAsync(pushToken);
                    }
                }
                else
                {
                    _logger.LogInformation($"Push notification sent successfully to {pushToken}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending push notification to device {pushToken}");
            }
        }

        public bool IsValidPushToken(string pushToken)
        {
            if (string.IsNullOrWhiteSpace(pushToken))
                return false;

            // Expo push tokens start with ExponentPushToken[ or ExpoPushToken[
            return pushToken.StartsWith("ExponentPushToken[") || 
                   pushToken.StartsWith("ExpoPushToken[");
        }

        private async Task DeactivateDeviceByTokenAsync(string pushToken)
        {
            try
            {
                var device = await _db.UserDevices
                    .FirstOrDefaultAsync(d => d.PushToken == pushToken);

                if (device != null)
                {
                    device.IsActive = false;
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Deactivated invalid device token: {pushToken}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating device token: {pushToken}");
            }
        }
    }
}
