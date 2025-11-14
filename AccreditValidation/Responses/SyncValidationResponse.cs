namespace AccreditValidation.Responses
{
    using System.Text.Json.Serialization;

    public class SyncValidationResponse
    {
        [JsonPropertyName("ValidationResults")]
        public List<ValidationResultItem> ValidationResults { get; set; } = new List<ValidationResultItem>();

        [JsonPropertyName("PageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("PageSize")]
        public int PageSize { get; set; }
    }

    public class ValidationResultItem
    {
        [JsonPropertyName("Barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("AreaId")]
        public string AreaId { get; set; }

        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("Mode")]
        public int Mode { get; set; }

        [JsonPropertyName("Direction")]
        public int Direction { get; set; }
    }
}
