namespace AccreditValidation.Requests
{
    using System.ComponentModel.DataAnnotations;

    public class BadgeValidationRequest
    {
        public string? Barcode { get; set; }

        public int? ContactID { get; set; }

        public string? ExternalID { get; set; }

        [Required]
        public string? AreaIdentifier { get; set; }

        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        public Enums.ValidationMode Mode { get; set; } = Enums.ValidationMode.Online;

        public Enums.ValidationDirection Direction { get; set; } = Enums.ValidationDirection.In;
    }
}
