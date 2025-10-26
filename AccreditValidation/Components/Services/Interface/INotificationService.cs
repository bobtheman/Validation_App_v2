using AccreditValidation.Shared.Services.Notification;

namespace AccreditValidation.Components.Services.Interface
{
    public interface INotificationService
    {
        Task<bool> RequestPermissionAsync();
        Task ShowNotificationAsync(string title, string message, int id = 0);
        Task CancelNotificationAsync(int id);
        Task CancelAllNotificationsAsync();

        // New methods for in-app notifications
        event EventHandler<InAppNotificationService> OnNotificationReceived;
        Task ShowInAppNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
        List<InAppNotificationService> GetInAppNotifications();
        Task MarkAsReadAsync(string notificationId);
        Task ClearInAppNotificationsAsync();
        int GetUnreadCount();
    }
}
