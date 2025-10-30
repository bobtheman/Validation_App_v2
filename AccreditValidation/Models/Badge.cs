namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;
    using SQLite;

    public class Badge
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonPropertyName("Barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("ContactId")]
        public int ContactId { get; set; }

        [JsonPropertyName("Forename")]
        public string? Forename { get; set; }

        [JsonPropertyName("Surname")]
        public string? Surname { get; set; }

        [JsonPropertyName("RegistrationSubTypeName")]
        public string? RegistrationSubTypeName { get; set; }

        [JsonPropertyName("AffiliatedOrganisationName")]
        public string? AffiliatedOrganisationName { get; set; }

        [JsonPropertyName("ResponsibleOrganisationName")]
        public string? ResponsibleOrganisationName { get; set; }

        [JsonPropertyName("Photo")]
        public string? Photo { get; set; }

        [JsonPropertyName("PhotoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("PhotoFileName")]
        public string? PhotoFileName { get; set; }

        public bool IsScanned { get; set; }

        public bool PhotoDownloaded { get; set; }
    }
}
