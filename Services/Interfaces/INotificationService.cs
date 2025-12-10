namespace Wihngo.Services.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;

    public interface INotificationService
    {
        /// <summary>
        /// Create and send a notification to a user
        /// </summary>
        Task<Notification> CreateNotificationAsync(CreateNotificationDto dto);

        /// <summary>
        /// Create multiple notifications (batch operation)
        /// </summary>
        Task<List<Notification>> CreateNotificationsAsync(List<CreateNotificationDto> dtos);

        /// <summary>
        /// Get all notifications for a user
        /// </summary>
        Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 20, 
            bool unreadOnly = false);

        /// <summary>
        /// Get unread count for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<int> MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Delete a notification
        /// </summary>
        Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Get notification preferences for a user
        /// </summary>
        Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId);

        /// <summary>
        /// Update notification preference
        /// </summary>
        Task<NotificationPreferenceDto> UpdatePreferenceAsync(
            Guid userId, 
            UpdateNotificationPreferenceDto dto);

        /// <summary>
        /// Get notification settings for a user
        /// </summary>
        Task<NotificationSettingsDto> GetSettingsAsync(Guid userId);

        /// <summary>
        /// Update notification settings
        /// </summary>
        Task<NotificationSettingsDto> UpdateSettingsAsync(Guid userId, NotificationSettingsDto dto);

        /// <summary>
        /// Check if user should receive notification based on preferences and limits
        /// </summary>
        Task<bool> CanSendNotificationAsync(Guid userId, NotificationType type, NotificationChannel channel);

        /// <summary>
        /// Check if within quiet hours for user
        /// </summary>
        Task<bool> IsQuietHoursAsync(Guid userId);

        /// <summary>
        /// Find or create a notification group for similar notifications
        /// </summary>
        Task<Guid?> FindOrCreateGroupAsync(
            Guid userId, 
            NotificationType type, 
            Guid? relatedEntityId);

        /// <summary>
        /// Send notification via specific channel
        /// </summary>
        Task SendViaChannelAsync(Notification notification, NotificationChannel channel);
    }
}
