namespace AccreditValidation.Shared.Services.Notification
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class InAppNotificationService : INotifyPropertyChanged
    {
        private bool _isRead;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                return $"{(int)span.TotalDays}d ago";
            }
        }

        public Color TypeColor => Type switch
        {
            NotificationType.Info => Color.FromArgb("#17a2b8"),
            NotificationType.Success => Color.FromArgb("#28a745"),
            NotificationType.Warning => Color.FromArgb("#ffc107"),
            NotificationType.Error => Color.FromArgb("#dc3545"),
            _ => Color.FromArgb("#6c757d")
        };

        public string TypeIcon => Type switch
        {
            NotificationType.Info => "ℹ️",
            NotificationType.Success => "✓",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "✕",
            _ => "•"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}