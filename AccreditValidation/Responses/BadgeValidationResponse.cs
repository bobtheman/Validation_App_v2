namespace AccreditValidation.Responses
{
    using System.Text.Json.Serialization;

    public class BadgeValidationResponse
    {
        [JsonPropertyName("ValidationResult")]
        public long ValidationResult { get; set; }

        [JsonPropertyName("ValidationResultName")]
        public string? ValidationResultName { get; set; }

        [JsonPropertyName("Badge")]
        public Models.Badge? Badge { get; set; }

        [JsonPropertyName("HttpStatusCode")]
        public long HttpStatusCode { get; set; }

        [JsonPropertyName("Errors")]
        public object[]? Errors { get; set; }
    }
}
