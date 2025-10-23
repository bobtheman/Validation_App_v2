namespace AccreditValidation.Models
{
    using AccreditValidation.AttributeHelper;

    public class ValidationDirection
    {
        [LocalizedDescription("Direction")]
        public string? Direction { get; set; }

        public string? Identifier { get; set; }

        public bool IsSelected { get; set; }
    }
}
