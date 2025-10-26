namespace AccreditValidation.Shared.Services.SignalRNotificationHub
{
    public class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
