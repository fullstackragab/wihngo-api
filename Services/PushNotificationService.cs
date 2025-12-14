namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Dapper;
    using FirebaseAdmin.Messaging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Wihngo.Data;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _oneSignalAppId;
        private readonly string? _oneSignalApiKey;

        public PushNotificationService(
            AppDbContext context,
            IDbConnectionFactory dbFactory,
            IConfiguration config,
            ILogger<PushNotificationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _dbFactory = dbFactory;
            _config = config;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _oneSignalAppId = _config["OneSignal:AppId"];
            _oneSignalApiKey = _config["OneSignal:ApiKey"];
        }

        public async Task SendPushNotificationAsync(Wihngo.Models.Notification notification)
        {
            try
            {
                // Get user's device tokens using raw SQL
                var sql = @"
                    SELECT * FROM user_devices
                    WHERE user_id = @UserId 
                      AND is_active = true 
                      AND push_token IS NOT NULL 
                      AND push_token != ''";
                
                var devices = await _dbFactory.QueryListAsync<UserDevice>(sql, new { UserId = notification.UserId });

                if (!devices.Any())
                {
                    _logger.LogInformation("No active devices found for user {UserId}", notification.UserId);
                    return;
                }

                foreach (var device in devices)
                {
                    try
                    {
                        if (device.DeviceType == "ios" || device.DeviceType == "android")
                        {
                            // Use Firebase Cloud Messaging for iOS/Android
                            await SendFcmNotificationAsync(device.PushToken!, notification);
                        }
                        else if (device.DeviceType == "web")
                        {
                            // Use OneSignal for web push notifications
                            await SendOneSignalNotificationAsync(device.PushToken!, notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send push notification to device {DeviceId}", device.DeviceId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification for notification {NotificationId}", notification.NotificationId);
            }
        }

        public async Task SendToDeviceAsync(string pushToken, string title, string message, object? data = null, int? badge = null)
        {
            try
            {
                var fcmMessage = new Message
                {
                    Token = pushToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = message
                    }
                };

                if (data != null)
                {
                    fcmMessage.Data = new Dictionary<string, string>
                    {
                        { "data", JsonSerializer.Serialize(data) }
                    };
                }

                if (badge.HasValue)
                {
                    fcmMessage.Apns = new ApnsConfig
                    {
                        Aps = new Aps { Badge = badge.Value }
                    };
                }

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                _logger.LogInformation("Successfully sent notification to device. Response: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to device {PushToken}", pushToken);
                throw;
            }
        }

        public bool IsValidPushToken(string pushToken)
        {
            // Basic validation for Expo push tokens
            if (string.IsNullOrWhiteSpace(pushToken))
                return false;

            // Expo push tokens start with ExponentPushToken[
            if (pushToken.StartsWith("ExponentPushToken["))
                return true;

            // Firebase tokens are typically 152+ characters
            if (pushToken.Length >= 152)
                return true;

            return false;
        }

        public async Task SendInvoiceIssuedNotificationAsync(Guid userId, string invoiceNumber)
        {
            try
            {
                var notification = new Wihngo.Models.Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = userId,
                    Type = Wihngo.Models.Enums.NotificationType.PaymentReceived,
                    Title = "Invoice Issued",
                    Message = $"Your invoice {invoiceNumber} has been issued and is ready for payment.",
                    Priority = Wihngo.Models.Enums.NotificationPriority.High,
                    Channels = Wihngo.Models.Enums.NotificationChannel.InApp | Wihngo.Models.Enums.NotificationChannel.Push,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await SendPushNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice notification for invoice {InvoiceNumber} to user {UserId}", invoiceNumber, userId);
                throw;
            }
        }

        private async Task SendFcmNotificationAsync(string token, Wihngo.Models.Notification notification)
        {
            try
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = notification.Title,
                        Body = notification.Message
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "notificationId", notification.NotificationId.ToString() },
                        { "type", notification.Type.ToString() },
                        { "createdAt", notification.CreatedAt.ToString("O") }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Successfully sent FCM notification. Response: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification to token {Token}", token);
                throw;
            }
        }

        private async Task SendOneSignalNotificationAsync(string playerId, Wihngo.Models.Notification notification)
        {
            try
            {
                if (string.IsNullOrEmpty(_oneSignalAppId) || string.IsNullOrEmpty(_oneSignalApiKey))
                {
                    _logger.LogWarning("OneSignal credentials not configured");
                    return;
                }

                var payload = new
                {
                    app_id = _oneSignalAppId,
                    include_player_ids = new[] { playerId },
                    headings = new { en = notification.Title },
                    contents = new { en = notification.Message },
                    data = new
                    {
                        notificationId = notification.NotificationId.ToString(),
                        type = notification.Type.ToString(),
                        createdAt = notification.CreatedAt.ToString("O")
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _oneSignalApiKey);

                var response = await _httpClient.PostAsync("https://onesignal.com/api/v1/notifications", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully sent OneSignal notification to player {PlayerId}", playerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OneSignal notification to player {PlayerId}", playerId);
                throw;
            }
        }
    }
}
