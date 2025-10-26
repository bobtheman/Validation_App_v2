using AccreditValidation.Shared.Services.Notification;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AccreditValidation.Components.Pages.Xaml;

public partial class NotificationPanel : ContentView
{
    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(NotificationPanel), false);

    public static readonly BindableProperty NotificationsProperty =
        BindableProperty.Create(nameof(Notifications), typeof(ObservableCollection<InAppNotificationService>), 
            typeof(NotificationPanel), new ObservableCollection<InAppNotificationService>(),
            propertyChanged: OnNotificationsChanged);

    public static readonly BindableProperty HasNotificationsProperty =
        BindableProperty.Create(nameof(HasNotifications), typeof(bool), typeof(NotificationPanel), false);

    private ObservableCollection<InAppNotificationService> _previousNotifications;

    public new bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public ObservableCollection<InAppNotificationService> Notifications
    {
        get => (ObservableCollection<InAppNotificationService>)GetValue(NotificationsProperty);
        set => SetValue(NotificationsProperty, value);
    }

    public bool HasNotifications
    {
        get => (bool)GetValue(HasNotificationsProperty);
        set => SetValue(HasNotificationsProperty, value);
    }

    public event EventHandler CloseRequested;
    public event EventHandler<InAppNotificationService> NotificationTapped;
    public event EventHandler MarkAllReadRequested;
    public event EventHandler ClearAllRequested;

    public NotificationPanel()
    {
        InitializeComponent();
    }

    private static void OnNotificationsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NotificationPanel panel)
        {
            // Unsubscribe from old collection
            if (panel._previousNotifications != null)
            {
                panel._previousNotifications.CollectionChanged -= panel.OnCollectionChanged;
            }

            if (newValue is ObservableCollection<InAppNotificationService> notifications)
            {
                panel.HasNotifications = notifications.Count > 0;
                
                // Subscribe to collection changes
                notifications.CollectionChanged += panel.OnCollectionChanged;
                panel._previousNotifications = notifications;
            }
            else
            {
                panel.HasNotifications = false;
            }
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        HasNotifications = Notifications?.Count > 0;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is InAppNotificationService notification)
        {
            // Mark the notification as read
            notification.IsRead = true;
            
            // Trigger the NotificationTapped event for parent components to handle
            NotificationTapped?.Invoke(this, notification);
            
            // Force UI refresh by triggering property changed
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var index = Notifications.IndexOf(notification);
                if (index >= 0)
                {
                    // Remove and re-add to trigger UI update
                    Notifications.RemoveAt(index);
                    Notifications.Insert(index, notification);
                }
            });
        }
    }

    private void OnMarkAllReadClicked(object sender, EventArgs e)
    {
        // Mark all notifications as read
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var temp = new List<InAppNotificationService>(Notifications);
            foreach (var notification in temp)
            {
                notification.IsRead = true;
            }
            
            // Raise the event for parent components
            MarkAllReadRequested?.Invoke(this, EventArgs.Empty);
            
            // Force UI refresh
            Notifications.Clear();
            foreach (var notification in temp)
            {
                Notifications.Add(notification);
            }
        });
    }

    private void OnClearAllClicked(object sender, EventArgs e)
    {
        ClearAllRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // Cleanup subscriptions when handler is removed
        if (Handler == null && _previousNotifications != null)
        {
            _previousNotifications.CollectionChanged -= OnCollectionChanged;
            _previousNotifications = null;
        }
    }
}