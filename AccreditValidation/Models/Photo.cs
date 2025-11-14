namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;

    public class Photo
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("FileName")]
        public string? PhotoFileName { get; set; }

        public string? PhotoUrl { get; set; }
    }
}
