namespace AccreditValidation.Requests
{
    using System.ComponentModel.DataAnnotations;

    public class BadgeValidationRequest
    {
        public string? Barcode { get; set; }

        [Required]
        public string? AreaId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Enums.ValidationMode Mode { get; set; } = Enums.ValidationMode.Online;

        public Enums.ValidationDirection Direction { get; set; } = Enums.ValidationDirection.In;
    }
}
