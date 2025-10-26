using AccreditValidation.Shared.Services.Notification;
using System.Collections.ObjectModel;

namespace AccreditValidation.Components.Base.Notification
{
    public partial class NotificationBellComponent
    {
        private ObservableCollection<InAppNotificationService> Notifications { get; set; } = new();
        private int UnreadCount { get; set; }
        private bool IsPanelVisible { get; set; }

        protected override void OnInitialized()
        {
            NotificationService.OnNotificationReceived += OnNotificationReceived;
            RefreshNotifications();
        }

        private void OnNotificationReceived(object? sender, InAppNotificationService notification)
        {
            InvokeAsync(() =>
            {
                RefreshNotifications();
                StateHasChanged();
            });
        }

        private void RefreshNotifications()
        {
            var notifications = NotificationService.GetInAppNotifications();
            Notifications.Clear();
            foreach (var n in notifications)
            {
                Notifications.Add(n);
            }
            UnreadCount = NotificationService.GetUnreadCount();
        }

        private void TogglePanel()
        {
            IsPanelVisible = !IsPanelVisible;
        }

        private void ClosePanel()
        {
            IsPanelVisible = false;
            StateHasChanged();
        }

        private async Task HandleNotificationTapped(InAppNotificationService notification)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
            RefreshNotifications();
            StateHasChanged();
        }

        private async Task HandleMarkAllRead()
        {
            foreach (var notification in Notifications)
            {
                await NotificationService.MarkAsReadAsync(notification.Id);
            }
            RefreshNotifications();
            StateHasChanged();
        }

        private async Task HandleClearAll()
        {
            await NotificationService.ClearInAppNotificationsAsync();
            RefreshNotifications();
            StateHasChanged();
        }

        public void Dispose()
        {
            NotificationService.OnNotificationReceived -= OnNotificationReceived;
        }
    }
}