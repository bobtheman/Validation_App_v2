using System.Text.Json.Serialization;

namespace AccreditValidation.Models
{
    public class ValidationResult
    {
        [JsonPropertyName("Badge")]
        public Badge Badge { get; set; }

        [JsonPropertyName("AreaValidationResults")]
        public AreaValidationResult[] AreaValidationResults { get; set; }
    }
}
