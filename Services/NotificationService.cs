namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Dapper;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IPushNotificationService _pushService;
        private readonly IEmailNotificationService _emailService;

        public NotificationService(
            AppDbContext db,
            IDbConnectionFactory dbFactory,
            IPushNotificationService pushService,
            IEmailNotificationService emailService)
        {
            _db = db;
            _dbFactory = dbFactory;
            _pushService = pushService;
            _emailService = emailService;
        }

        public async Task<Notification> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var canSend = await CanSendNotificationAsync(dto.UserId, dto.Type, dto.Channels);
            if (!canSend && dto.Priority != NotificationPriority.Critical)
            {
                throw new InvalidOperationException("User has reached notification limit or disabled this notification type");
            }

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
                var sql = @"
                    SELECT * FROM notifications
                    WHERE group_id = @GroupId 
                      AND user_id = @UserId 
                      AND is_read = false
                    ORDER BY created_at DESC
                    LIMIT 1";
                
                var existingGroup = await _dbFactory.QuerySingleOrDefaultAsync<Notification>(sql, new 
                { 
                    GroupId = groupId, 
                    UserId = dto.UserId 
                });

                if (existingGroup != null)
                {
                    existingGroup.GroupCount++;
                    existingGroup.Message = dto.Message;
                    existingGroup.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

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
            // Build base SQL with optional unread filter
            var countSql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId";
            var dataSql = @"
                SELECT 
                    notification_id as NotificationId,
                    type as Type,
                    title as Title,
                    message as Message,
                    priority as Priority,
                    is_read as IsRead,
                    read_at as ReadAt,
                    deep_link as DeepLink,
                    bird_id as BirdId,
                    story_id as StoryId,
                    actor_user_id as ActorUserId,
                    group_count as GroupCount,
                    created_at as CreatedAt
                FROM notifications
                WHERE user_id = @UserId";

            if (unreadOnly)
            {
                countSql += " AND is_read = false";
                dataSql += " AND is_read = false";
            }

            dataSql += " ORDER BY created_at DESC OFFSET @Offset LIMIT @Limit";

            var totalCount = await _dbFactory.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
            var notifications = await _dbFactory.QueryListAsync<NotificationDto>(dataSql, new 
            { 
                UserId = userId,
                Offset = (page - 1) * pageSize,
                Limit = pageSize
            });

            // Load actor user names
            var actorUserIds = notifications
                .Where(n => n.ActorUserId.HasValue)
                .Select(n => n.ActorUserId!.Value)
                .Distinct()
                .ToList();

            if (actorUserIds.Any())
            {
                var actorSql = "SELECT user_id, name FROM users WHERE user_id = ANY(@UserIds)";
                var connection = await _dbFactory.CreateOpenConnectionAsync();
                try
                {
                    var actors = await connection.QueryAsync<(Guid UserId, string Name)>(actorSql, new { UserIds = actorUserIds.ToArray() });
                    var actorDict = actors.ToDictionary(a => a.UserId, a => a.Name);

                    foreach (var notification in notifications)
                    {
                        if (notification.ActorUserId.HasValue && actorDict.ContainsKey(notification.ActorUserId.Value))
                        {
                            notification.ActorUserName = actorDict[notification.ActorUserId.Value];
                        }
                    }
                }
                finally
                {
                    await connection.DisposeAsync();
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
            var sql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId AND is_read = false";
            return await _dbFactory.ExecuteScalarAsync<int>(sql, new { UserId = userId });
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
            var sql = @"
                SELECT * FROM notifications
                WHERE user_id = @UserId AND is_read = false";
            
            var unreadNotifications = await _dbFactory.QueryListAsync<Notification>(sql, new { UserId = userId });

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

            var sql = @"
                SELECT 
                    preference_id as PreferenceId,
                    notification_type as NotificationType,
                    in_app_enabled as InAppEnabled,
                    push_enabled as PushEnabled,
                    email_enabled as EmailEnabled,
                    sms_enabled as SmsEnabled
                FROM notification_preferences
                WHERE user_id = @UserId";

            return await _dbFactory.QueryListAsync<NotificationPreferenceDto>(sql, new { UserId = userId });
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
                    var pushCountSql = @"
                        SELECT COUNT(*) FROM notifications
                        WHERE user_id = @UserId 
                          AND push_sent = true 
                          AND created_at >= @Today";
                    
                    var pushCount = await _dbFactory.ExecuteScalarAsync<int>(pushCountSql, new 
                    { 
                        UserId = userId, 
                        Today = today 
                    });

                    if (pushCount >= settings.MaxPushPerDay)
                        return false;
                }

                if (channel.HasFlag(NotificationChannel.Email))
                {
                    var emailCountSql = @"
                        SELECT COUNT(*) FROM notifications
                        WHERE user_id = @UserId 
                          AND email_sent = true 
                          AND created_at >= @Today";
                    
                    var emailCount = await _dbFactory.ExecuteScalarAsync<int>(emailCountSql, new 
                    { 
                        UserId = userId, 
                        Today = today 
                    });

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
            
            var sql = @"
                SELECT group_id FROM notifications
                WHERE user_id = @UserId 
                  AND type = @Type
                  AND (bird_id = @EntityId OR story_id = @EntityId)
                  AND is_read = false
                  AND created_at >= @WindowStart
                LIMIT 1";
            
            var existingGroup = await _dbFactory.ExecuteScalarAsync<Guid?>(sql, new 
            { 
                UserId = userId,
                Type = type.ToString(),
                EntityId = relatedEntityId,
                WindowStart = windowStart
            });

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
            var countSql = "SELECT COUNT(*) FROM notification_preferences WHERE user_id = @UserId";
            var existingCount = await _dbFactory.ExecuteScalarAsync<int>(countSql, new { UserId = userId });

            var allTypes = Enum.GetValues<NotificationType>();

            if (existingCount < allTypes.Length)
            {
                var typesSql = "SELECT notification_type FROM notification_preferences WHERE user_id = @UserId";
                var existingTypes = await _dbFactory.QueryListAsync<string>(typesSql, new { UserId = userId });
                var existingTypeEnums = existingTypes.Select(t => Enum.Parse<NotificationType>(t)).ToList();

                var missingTypes = allTypes.Except(existingTypeEnums);

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
