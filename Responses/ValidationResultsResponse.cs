namespace AccreditValidation.Responses
{
    using System.Text.Json.Serialization;

    public class ValidationResultsResponse
    {
        [JsonPropertyName("TotalRecords")]
        public long TotalRecords { get; set; }

        [JsonPropertyName("TotalPages")]
        public long TotalPages { get; set; }

        [JsonPropertyName("PageSize")]
        public long PageSize { get; set; }

        [JsonPropertyName("PageNumber")]
        public long PageNumber { get; set; }

        [JsonPropertyName("Data")]
        public ValidationData[] Data { get; set; }
    }

    public class ValidationData
    {
        [JsonPropertyName("Badge")]
        public Models.Badge? Badge { get; set; }

        [JsonPropertyName("AreaValidationResults")]
        public List<Models.AreaValidationResult>? AreaValidationResults { get; set; }
    }
}
