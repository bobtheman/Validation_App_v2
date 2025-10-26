using AccreditValidation.Shared.Services.Notification;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AccreditValidation.Components.Base.Notification
{
    public partial class NotificationPanelComponent : IDisposable
    {
        private ObservableCollection<InAppNotificationService>? _previousNotifications;

        [Parameter]
        public ObservableCollection<InAppNotificationService>? Notifications { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public EventCallback<InAppNotificationService> OnNotificationTapped { get; set; }

        [Parameter]
        public EventCallback OnMarkAllRead { get; set; }

        [Parameter]
        public EventCallback OnClearAll { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // Unsubscribe from old collection
            if (_previousNotifications != null)
            {
                _previousNotifications.CollectionChanged -= OnCollectionChanged;
            }

            // Subscribe to new collection
            if (Notifications != null)
            {
                Notifications.CollectionChanged += OnCollectionChanged;
                _previousNotifications = Notifications;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task ClosePanel()
        {
            await OnClose.InvokeAsync();
        }

        private async Task OnNotificationTappedHandler(InAppNotificationService notification)
        {
            notification.IsRead = true;
            await OnNotificationTapped.InvokeAsync(notification);
            StateHasChanged();
        }

        private async Task OnMarkAllReadClicked()
        {
            if (Notifications != null)
            {
                foreach (var notification in Notifications)
                {
                    notification.IsRead = true;
                }
            }
            await OnMarkAllRead.InvokeAsync();
            StateHasChanged();
        }

        private async Task OnClearAllClicked()
        {
            await OnClearAll.InvokeAsync();
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
            if (_previousNotifications != null)
            {
                _previousNotifications.CollectionChanged -= OnCollectionChanged;
                _previousNotifications = null;
            }
        }
    }
}