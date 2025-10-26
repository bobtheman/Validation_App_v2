namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Shared.Services.Notification;
    using System.Collections.ObjectModel;

#if ANDROID
    using Android.App;
    using Android.Content;
    using Android.OS;
    using AndroidX.Core.App;
    using Microsoft.Maui.ApplicationModel;

    public class NotificationService : INotificationService
    {
        private const string ChannelId = "default_channel";
        private const string ChannelName = "Default Notifications";
        private readonly ObservableCollection<InAppNotificationService> _inAppNotifications = new();
        public event EventHandler<InAppNotificationService> OnNotificationReceived;

        public async Task<bool> RequestPermissionAsync()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                return status == PermissionStatus.Granted;
            }
            return true;
        }

        public Task ShowNotificationAsync(string title, string message, int id = 0)
        {
            var context = Platform.CurrentActivity ?? Application.Context;
            if (context == null)
            {
                return Task.CompletedTask;
            }

            CreateNotificationChannel(context);

            var intent = new Intent(context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                0,
                intent,
                PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetSmallIcon(Resource.Drawable.notification_bg_normal)
                .SetPriority(NotificationCompat.PriorityDefault)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true);

            var notificationManager = NotificationManagerCompat.From(context);
            if (notificationManager != null)
            {
                notificationManager.Notify(id, builder.Build());
            }

            return Task.CompletedTask;
        }

        public Task CancelNotificationAsync(int id)
        {
            var context = Platform.CurrentActivity ?? Application.Context;
            if (context == null)
            {
                return Task.CompletedTask;
            }

            var notificationManager = NotificationManagerCompat.From(context);
            if (notificationManager != null)
            {
                notificationManager.Cancel(id);
            }
            return Task.CompletedTask;
        }

        public Task CancelAllNotificationsAsync()
        {
            var context = Platform.CurrentActivity ?? Application.Context;
            if (context == null)
            {
                return Task.CompletedTask;
            }

            var notificationManager = NotificationManagerCompat.From(context);
            if (notificationManager != null)
            {
                notificationManager.CancelAll();
            }
            return Task.CompletedTask;
        }

        public Task ShowInAppNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            var notification = new InAppNotificationService
            {
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _inAppNotifications.Insert(0, notification);
            OnNotificationReceived?.Invoke(this, notification);

            return Task.CompletedTask;
        }

        public List<InAppNotificationService> GetInAppNotifications()
        {
            return _inAppNotifications.ToList();
        }

        public Task MarkAsReadAsync(string notificationId)
        {
            var notification = _inAppNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                // Force collection change notification
                var index = _inAppNotifications.IndexOf(notification);
                _inAppNotifications.RemoveAt(index);
                _inAppNotifications.Insert(index, notification);
            }
            return Task.CompletedTask;
        }

        public Task ClearInAppNotificationsAsync()
        {
            _inAppNotifications.Clear();
            return Task.CompletedTask;
        }

        public int GetUnreadCount()
        {
            return _inAppNotifications.Count(n => !n.IsRead);
        }

        private void CreateNotificationChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                var channel = new NotificationChannel(
                    ChannelId,
                    ChannelName,
                    NotificationImportance.Default)
                {
                    Description = "Default notification channel"
                };

                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
#pragma warning restore CA1416
            }
        }
    }
#elif IOS
    using Foundation;
    using UserNotifications;

    public class NotificationService : INotificationService
    {
        private readonly ObservableCollection<InAppNotificationService> _inAppNotifications = new();
        public event EventHandler<InAppNotificationService> OnNotificationReceived;

        public async Task<bool> RequestPermissionAsync()
        {
            var center = UNUserNotificationCenter.Current;
            var (granted, error) = await center.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | 
                UNAuthorizationOptions.Badge | 
                UNAuthorizationOptions.Sound);
            
            return granted;
        }

        public async Task ShowNotificationAsync(string title, string message, int id = 0)
        {
            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = message,
                Sound = UNNotificationSound.Default
            };

            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.1, false);
            var request = UNNotificationRequest.FromIdentifier(
                id.ToString(), 
                content, 
                trigger);

            var center = UNUserNotificationCenter.Current;
            await center.AddNotificationRequestAsync(request);
        }

        public async Task CancelNotificationAsync(int id)
        {
            var center = UNUserNotificationCenter.Current;
            center.RemovePendingNotificationRequests(new[] { id.ToString() });
            center.RemoveDeliveredNotifications(new[] { id.ToString() });
            await Task.CompletedTask;
        }

        public async Task CancelAllNotificationsAsync()
        {
            var center = UNUserNotificationCenter.Current;
            center.RemoveAllPendingNotificationRequests();
            center.RemoveAllDeliveredNotifications();
            await Task.CompletedTask;
        }

        public Task ShowInAppNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            var notification = new InAppNotificationService
            {
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _inAppNotifications.Insert(0, notification);
            OnNotificationReceived?.Invoke(this, notification);

            return Task.CompletedTask;
        }

        public List<InAppNotificationService> GetInAppNotifications()
        {
            return _inAppNotifications.ToList();
        }

        public Task MarkAsReadAsync(string notificationId)
        {
            var notification = _inAppNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
            return Task.CompletedTask;
        }

        public Task ClearInAppNotificationsAsync()
        {
            _inAppNotifications.Clear();
            return Task.CompletedTask;
        }

        public int GetUnreadCount()
        {
            return _inAppNotifications.Count(n => !n.IsRead);
        }
    }
#elif WINDOWS
    using Microsoft.Windows.AppNotifications;
    using Microsoft.Windows.AppNotifications.Builder;

    public class NotificationService : INotificationService
    {
        private readonly ObservableCollection<InAppNotificationService> _inAppNotifications = new();
        public event EventHandler<InAppNotificationService> OnNotificationReceived;

        public Task<bool> RequestPermissionAsync()
        {
            return Task.FromResult(true);
        }

        public Task ShowNotificationAsync(string title, string message, int id = 0)
        {
            var builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message);

            var notification = builder.BuildNotification();
            notification.Tag = id.ToString();
            
            AppNotificationManager.Default.Show(notification);
            
            return Task.CompletedTask;
        }

        public async Task CancelNotificationAsync(int id)
        {
            await AppNotificationManager.Default.RemoveByTagAsync(id.ToString());
        }

        public async Task CancelAllNotificationsAsync()
        {
            await AppNotificationManager.Default.RemoveAllAsync();
        }

        public Task ShowInAppNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            var notification = new InAppNotificationService
            {
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _inAppNotifications.Insert(0, notification);
            OnNotificationReceived?.Invoke(this, notification);

            return Task.CompletedTask;
        }

        public List<InAppNotificationService> GetInAppNotifications()
        {
            return _inAppNotifications.ToList();
        }

        public Task MarkAsReadAsync(string notificationId)
        {
            var notification = _inAppNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
            return Task.CompletedTask;
        }

        public Task ClearInAppNotificationsAsync()
        {
            _inAppNotifications.Clear();
            return Task.CompletedTask;
        }

        public int GetUnreadCount()
        {
            return _inAppNotifications.Count(n => !n.IsRead);
        }
    }
#endif
}