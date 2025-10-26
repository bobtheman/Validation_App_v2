namespace AccreditValidation.Shared.Services.Notification
{
    public class InAppNotificationModel
    {
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime Timestamp { get; set; }

        // Computed property for display
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                
                return Timestamp.ToString("MMM dd");
            }
        }

        // Computed property for icon based on type
        public string TypeIcon
        {
            get
            {
                return Type switch
                {
                    NotificationType.Info => "ℹ",
                    NotificationType.Success => "✓",
                    NotificationType.Warning => "⚠",
                    NotificationType.Error => "✕",
                    _ => "•"
                };
            }
        }

        // Computed property for color based on type
        public System.Drawing.Color TypeColor
        {
            get
            {
                return Type switch
                {
                    NotificationType.Info => System.Drawing.Color.Blue,
                    NotificationType.Success => System.Drawing.Color.Green,
                    NotificationType.Warning => System.Drawing.Color.Orange,
                    NotificationType.Error => System.Drawing.Color.Red,
                    _ => System.Drawing.Color.Gray
                };
            }
        }
    }
}
