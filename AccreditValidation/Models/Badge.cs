namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;
    using SQLite;


    public class Badge
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonPropertyName("Barcode")]
        public string? Barcode { get; set; }

        [JsonPropertyName("RegistrationId")]
        public string? RegistrationId { get; set; }

        [JsonPropertyName("RfidCode")]
        public string? RfidCode { get; set; }

        [JsonPropertyName("ExternalId")]
        public string? ExternalId { get; set; }

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

        [JsonPropertyName("IsSeasonPass")]
        public bool IsSeasonPass { get; set; }

        [JsonPropertyName("IsTenantSeasonPass")]
        public bool IsTenantSeasonPass { get; set; }

        [JsonPropertyName("EventId")]
        public int EventId { get; set; }

        [JsonPropertyName("EventName")]
        public string? EventName { get; set; }

        [JsonPropertyName("Photo")]
        [Ignore]
        public Photo? Photo { get; set; }   

        // Flattened Photo properties for SQLite storage
        public string? PhotoId { get; set; }
        public string? PhotoFileName { get; set; }
        public string? PhotoUrl { get; set; }

        [Ignore]
        public virtual ContactImage? ContactImage { get; set; }

        public bool IsScanned { get; set; }

        public bool PhotoDownloaded { get; set; }
    }
}
