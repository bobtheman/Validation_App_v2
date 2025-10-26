using AccreditValidation.Shared.Services.Notification;
using System.Collections.ObjectModel;

namespace AccreditValidation.Components.Notification
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
        }

        private async Task OnNotificationTapped(InAppNotificationService notification)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
            RefreshNotifications();
            StateHasChanged();
        }

        private async Task OnMarkAllRead()
        {
            foreach (var notification in Notifications)
            {
                await NotificationService.MarkAsReadAsync(notification.Id);
            }
            RefreshNotifications();
            StateHasChanged();
        }

        private async Task OnClearAll()
        {
            await NotificationService.ClearInAppNotificationsAsync();
            RefreshNotifications();
            StateHasChanged();
        }

        private string GetTypeColor(NotificationType type) => type switch
        {
            NotificationType.Info => "#17a2b8",
            NotificationType.Success => "#28a745",
            NotificationType.Warning => "#ffc107",
            NotificationType.Error => "#dc3545",
            _ => "#6c757d"
        };

        private string GetTypeIcon(NotificationType type) => type switch
        {
            NotificationType.Info => "ℹ️",
            NotificationType.Success => "✓",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "✕",
            _ => "•"
        };

        private string GetTimeAgo(DateTime timestamp)
        {
            var span = DateTime.Now - timestamp;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }

        public void Dispose()
        {
            NotificationService.OnNotificationReceived -= OnNotificationReceived;
        }
    }
}