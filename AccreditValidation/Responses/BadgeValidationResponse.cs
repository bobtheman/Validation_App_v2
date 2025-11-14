namespace AccreditValidation.Responses
{
    using System.Text.Json.Serialization;

    public class BadgeValidationResponse
    {
        [JsonPropertyName("Code")]
        public long Code { get; set; }

        [JsonPropertyName("Result")]
        public string Result { get; set; }

        [JsonPropertyName("Badge")]
        public Models.Badge? Badge { get; set; }

        [JsonPropertyName("HttpStatusCode")]
        public long HttpStatusCode { get; set; }

        public int ValidationResult { get; set; }
    }
}
