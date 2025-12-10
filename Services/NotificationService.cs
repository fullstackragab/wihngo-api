namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IPushNotificationService _pushService;
        private readonly IEmailNotificationService _emailService;

        public NotificationService(
            AppDbContext db,
            IPushNotificationService pushService,
            IEmailNotificationService emailService)
        {
            _db = db;
            _pushService = pushService;
            _emailService = emailService;
        }

        public async Task<Notification> CreateNotificationAsync(CreateNotificationDto dto)
        {
            // Check if user can receive this notification
            var canSend = await CanSendNotificationAsync(dto.UserId, dto.Type, dto.Channels);
            if (!canSend && dto.Priority != NotificationPriority.Critical)
            {
                throw new InvalidOperationException("User has reached notification limit or disabled this notification type");
            }

            // Check for grouping
            Guid? groupId = null;
            if (dto.BirdId.HasValue || dto.StoryId.HasValue)
            {
                groupId = await FindOrCreateGroupAsync(dto.UserId, dto.Type, dto.BirdId ?? dto.StoryId);
            }

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = dto.UserId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                Priority = dto.Priority,
                Channels = dto.Channels,
                DeepLink = dto.DeepLink,
                BirdId = dto.BirdId,
                StoryId = dto.StoryId,
                TransactionId = dto.TransactionId,
                ActorUserId = dto.ActorUserId,
                GroupId = groupId,
                Metadata = dto.Metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // If this is part of a group, update existing group notification
            if (groupId.HasValue)
            {
                var existingGroup = await _db.Notifications
                    .Where(n => n.GroupId == groupId && n.UserId == dto.UserId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingGroup != null)
                {
                    existingGroup.GroupCount++;
                    existingGroup.Message = dto.Message; // Update with latest message
                    existingGroup.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    // Send updated notification
                    await SendNotificationChannelsAsync(existingGroup);
                    return existingGroup;
                }
            }

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Send notification through channels (async, don't wait)
            _ = Task.Run(async () => await SendNotificationChannelsAsync(notification));

            return notification;
        }

        public async Task<List<Notification>> CreateNotificationsAsync(List<CreateNotificationDto> dtos)
        {
            var notifications = new List<Notification>();
            foreach (var dto in dtos)
            {
                try
                {
                    var notification = await CreateNotificationAsync(dto);
                    notifications.Add(notification);
                }
                catch
                {
                    // Skip failed notifications
                    continue;
                }
            }
            return notifications;
        }

        public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 20, 
            bool unreadOnly = false)
        {
            var query = _db.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    Priority = n.Priority,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    DeepLink = n.DeepLink,
                    BirdId = n.BirdId,
                    StoryId = n.StoryId,
                    ActorUserId = n.ActorUserId,
                    GroupCount = n.GroupCount,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            // Load actor user names
            var actorUserIds = notifications
                .Where(n => n.ActorUserId.HasValue)
                .Select(n => n.ActorUserId!.Value)
                .Distinct()
                .ToList();

            if (actorUserIds.Any())
            {
                var actors = await _db.Users
                    .Where(u => actorUserIds.Contains(u.UserId))
                    .Select(u => new { u.UserId, u.Name })
                    .ToListAsync();

                var actorDict = actors.ToDictionary(a => a.UserId, a => a.Name);

                foreach (var notification in notifications)
                {
                    if (notification.ActorUserId.HasValue && actorDict.ContainsKey(notification.ActorUserId.Value))
                    {
                        notification.ActorUserName = actorDict[notification.ActorUserId.Value];
                    }
                }
            }

            return new PagedResult<NotificationDto>
            {
                Items = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null || notification.IsRead)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var unreadNotifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            var count = unreadNotifications.Count;
            var now = DateTime.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _db.SaveChangesAsync();
            return count;
        }

        public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId)
        {
            // Ensure user has all preferences initialized
            await EnsureUserPreferencesAsync(userId);

            return await _db.NotificationPreferences
                .Where(np => np.UserId == userId)
                .Select(np => new NotificationPreferenceDto
                {
                    PreferenceId = np.PreferenceId,
                    NotificationType = np.NotificationType,
                    InAppEnabled = np.InAppEnabled,
                    PushEnabled = np.PushEnabled,
                    EmailEnabled = np.EmailEnabled,
                    SmsEnabled = np.SmsEnabled
                })
                .ToListAsync();
        }

        public async Task<NotificationPreferenceDto> UpdatePreferenceAsync(
            Guid userId, 
            UpdateNotificationPreferenceDto dto)
        {
            var preference = await _db.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId && np.NotificationType == dto.NotificationType);

            if (preference == null)
            {
                preference = new NotificationPreference
                {
                    PreferenceId = Guid.NewGuid(),
                    UserId = userId,
                    NotificationType = dto.NotificationType,
                    InAppEnabled = dto.InAppEnabled ?? true,
                    PushEnabled = dto.PushEnabled ?? true,
                    EmailEnabled = dto.EmailEnabled ?? true,
                    SmsEnabled = dto.SmsEnabled ?? false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.NotificationPreferences.Add(preference);
            }
            else
            {
                if (dto.InAppEnabled.HasValue) preference.InAppEnabled = dto.InAppEnabled.Value;
                if (dto.PushEnabled.HasValue) preference.PushEnabled = dto.PushEnabled.Value;
                if (dto.EmailEnabled.HasValue) preference.EmailEnabled = dto.EmailEnabled.Value;
                if (dto.SmsEnabled.HasValue) preference.SmsEnabled = dto.SmsEnabled.Value;
                preference.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return new NotificationPreferenceDto
            {
                PreferenceId = preference.PreferenceId,
                NotificationType = preference.NotificationType,
                InAppEnabled = preference.InAppEnabled,
                PushEnabled = preference.PushEnabled,
                EmailEnabled = preference.EmailEnabled,
                SmsEnabled = preference.SmsEnabled
            };
        }

        public async Task<NotificationSettingsDto> GetSettingsAsync(Guid userId)
        {
            var settings = await _db.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.UserId == userId);

            if (settings == null)
            {
                // Create default settings
                settings = new NotificationSettings
                {
                    SettingsId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _db.NotificationSettings.Add(settings);
                await _db.SaveChangesAsync();
            }

            return new NotificationSettingsDto
            {
                SettingsId = settings.SettingsId,
                QuietHoursStart = settings.QuietHoursStart,
                QuietHoursEnd = settings.QuietHoursEnd,
                QuietHoursEnabled = settings.QuietHoursEnabled,
                MaxPushPerDay = settings.MaxPushPerDay,
                MaxEmailPerDay = settings.MaxEmailPerDay,
                EnableNotificationGrouping = settings.EnableNotificationGrouping,
                GroupingWindowMinutes = settings.GroupingWindowMinutes,
                EnableDailyDigest = settings.EnableDailyDigest,
                DailyDigestTime = settings.DailyDigestTime,
                TimeZone = settings.TimeZone
            };
        }

        public async Task<NotificationSettingsDto> UpdateSettingsAsync(Guid userId, NotificationSettingsDto dto)
        {
            var settings = await _db.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.UserId == userId);

            if (settings == null)
            {
                settings = new NotificationSettings
                {
                    SettingsId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _db.NotificationSettings.Add(settings);
            }

            settings.QuietHoursStart = dto.QuietHoursStart;
            settings.QuietHoursEnd = dto.QuietHoursEnd;
            settings.QuietHoursEnabled = dto.QuietHoursEnabled;
            settings.MaxPushPerDay = dto.MaxPushPerDay;
            settings.MaxEmailPerDay = dto.MaxEmailPerDay;
            settings.EnableNotificationGrouping = dto.EnableNotificationGrouping;
            settings.GroupingWindowMinutes = dto.GroupingWindowMinutes;
            settings.EnableDailyDigest = dto.EnableDailyDigest;
            settings.DailyDigestTime = dto.DailyDigestTime;
            settings.TimeZone = dto.TimeZone;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> CanSendNotificationAsync(Guid userId, NotificationType type, NotificationChannel channel)
        {
            // Critical notifications always go through
            if (channel == NotificationChannel.None)
                return true;

            // Check user preferences
            var preference = await _db.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId && np.NotificationType == type);

            if (preference != null)
            {
                if (channel.HasFlag(NotificationChannel.InApp) && !preference.InAppEnabled)
                    return false;
                if (channel.HasFlag(NotificationChannel.Push) && !preference.PushEnabled)
                    return false;
                if (channel.HasFlag(NotificationChannel.Email) && !preference.EmailEnabled)
                    return false;
                if (channel.HasFlag(NotificationChannel.Sms) && !preference.SmsEnabled)
                    return false;
            }

            // Check daily limits
            var settings = await _db.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.UserId == userId);

            if (settings != null)
            {
                var today = DateTime.UtcNow.Date;
                
                if (channel.HasFlag(NotificationChannel.Push))
                {
                    var pushCount = await _db.Notifications
                        .Where(n => n.UserId == userId && 
                                    n.PushSent && 
                                    n.CreatedAt >= today)
                        .CountAsync();

                    if (pushCount >= settings.MaxPushPerDay)
                        return false;
                }

                if (channel.HasFlag(NotificationChannel.Email))
                {
                    var emailCount = await _db.Notifications
                        .Where(n => n.UserId == userId && 
                                    n.EmailSent && 
                                    n.CreatedAt >= today)
                        .CountAsync();

                    if (emailCount >= settings.MaxEmailPerDay)
                        return false;
                }
            }

            return true;
        }

        public async Task<bool> IsQuietHoursAsync(Guid userId)
        {
            var settings = await _db.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.UserId == userId);

            if (settings == null || !settings.QuietHoursEnabled)
                return false;

            var now = DateTime.UtcNow.TimeOfDay;
            var start = settings.QuietHoursStart;
            var end = settings.QuietHoursEnd;

            // Handle quiet hours that span midnight
            if (start < end)
            {
                return now >= start && now < end;
            }
            else
            {
                return now >= start || now < end;
            }
        }

        public async Task<Guid?> FindOrCreateGroupAsync(
            Guid userId, 
            NotificationType type, 
            Guid? relatedEntityId)
        {
            if (!relatedEntityId.HasValue)
                return null;

            var settings = await _db.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.UserId == userId);

            if (settings == null || !settings.EnableNotificationGrouping)
                return null;

            // Find recent similar notification within grouping window
            var windowStart = DateTime.UtcNow.AddMinutes(-settings.GroupingWindowMinutes);
            
            var existingGroup = await _db.Notifications
                .Where(n => n.UserId == userId && 
                            n.Type == type && 
                            (n.BirdId == relatedEntityId || n.StoryId == relatedEntityId) &&
                            !n.IsRead &&
                            n.CreatedAt >= windowStart)
                .Select(n => n.GroupId)
                .FirstOrDefaultAsync();

            return existingGroup ?? Guid.NewGuid();
        }

        public async Task SendViaChannelAsync(Notification notification, NotificationChannel channel)
        {
            if (channel.HasFlag(NotificationChannel.Push))
            {
                await _pushService.SendPushNotificationAsync(notification);
                notification.PushSent = true;
                notification.PushSentAt = DateTime.UtcNow;
            }

            if (channel.HasFlag(NotificationChannel.Email))
            {
                await _emailService.SendEmailNotificationAsync(notification);
                notification.EmailSent = true;
                notification.EmailSentAt = DateTime.UtcNow;
            }

            // SMS not implemented yet
            if (channel.HasFlag(NotificationChannel.Sms))
            {
                notification.SmsSent = false;
            }

            await _db.SaveChangesAsync();
        }

        private async Task SendNotificationChannelsAsync(Notification notification)
        {
            // Check quiet hours for non-critical notifications
            if (notification.Priority != NotificationPriority.Critical)
            {
                var isQuietHours = await IsQuietHoursAsync(notification.UserId);
                if (isQuietHours)
                {
                    // Skip push and SMS during quiet hours, only email and in-app
                    notification.Channels &= ~(NotificationChannel.Push | NotificationChannel.Sms);
                }
            }

            await SendViaChannelAsync(notification, notification.Channels);
        }

        private async Task EnsureUserPreferencesAsync(Guid userId)
        {
            var existingCount = await _db.NotificationPreferences
                .Where(np => np.UserId == userId)
                .CountAsync();

            var allTypes = Enum.GetValues<NotificationType>();

            if (existingCount < allTypes.Length)
            {
                var existingTypes = await _db.NotificationPreferences
                    .Where(np => np.UserId == userId)
                    .Select(np => np.NotificationType)
                    .ToListAsync();

                var missingTypes = allTypes.Except(existingTypes);

                foreach (var type in missingTypes)
                {
                    _db.NotificationPreferences.Add(new NotificationPreference
                    {
                        PreferenceId = Guid.NewGuid(),
                        UserId = userId,
                        NotificationType = type,
                        InAppEnabled = true,
                        PushEnabled = true,
                        EmailEnabled = GetDefaultEmailEnabled(type),
                        SmsEnabled = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
            }
        }

        private bool GetDefaultEmailEnabled(NotificationType type)
        {
            // Only enable email for high-priority notifications by default
            return type switch
            {
                NotificationType.BirdSupported => true,
                NotificationType.PaymentReceived => true,
                NotificationType.SecurityAlert => true,
                NotificationType.PremiumExpiring => true,
                NotificationType.BirdMemorial => true,
                _ => false
            };
        }
    }
}
