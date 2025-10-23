namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;
    using SQLite;

    public class AreaValidationResult
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonPropertyName("AreaIdentifier")]
        public string? AreaIdentifier { get; set; }

        [JsonPropertyName("ValidationResult")]
        public int ValidationResult { get; set; }

        [JsonPropertyName("ValidationResultName")]
        public string? ValidationResultName { get; set; }

        [JsonIgnore]
        public int Barcode { get; set; }
    }
}
