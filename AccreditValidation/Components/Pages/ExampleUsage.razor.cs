using AccreditValidation.Components.Base.Notification;
using AccreditValidation.Shared.Services.Notification;

namespace AccreditValidation.Components.Pages
{
    public partial class ExampleUsage
    {
        private NotificationBellComponent? notificationBell;

        protected override async Task OnInitializedAsync()
        {
            // Subscribe to notification events
            NotificationService.OnNotificationReceived += OnNotificationReceived;

            // Request permission
            await NotificationService.RequestPermissionAsync();
        }

        private void OnNotificationReceived(object? sender, InAppNotificationService notification)
        {
            // Update the notification bell
            InvokeAsync(StateHasChanged);
        }

        private async Task SendInfoNotification()
        {
            await NotificationService.ShowInAppNotificationAsync(
                "Information",
                "This is an informational message",
                NotificationType.Info);
        }

        private async Task SendSuccessNotification()
        {
            await NotificationService.ShowInAppNotificationAsync(
                "Success",
                "Operation completed successfully!",
                NotificationType.Success);
        }

        private async Task SendWarningNotification()
        {
            await NotificationService.ShowInAppNotificationAsync(
                "Warning",
                "Please review this important information",
                NotificationType.Warning);
        }

        private async Task SendErrorNotification()
        {
            await NotificationService.ShowInAppNotificationAsync(
                "Error",
                "An error occurred during the operation",
                NotificationType.Error);
        }

        public void Dispose()
        {
            NotificationService.OnNotificationReceived -= OnNotificationReceived;
        }
    }
}