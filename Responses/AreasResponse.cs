namespace AccreditValidation.Responses
{
    using AccreditValidation.Models;
    using System.Text.Json.Serialization;

    public class AreasResponse
    {
        [JsonPropertyName("Areas")]
        public Area[]? Areas { get; set; }

        [JsonPropertyName("HttpStatusCode")]
        public long HttpStatusCode { get; set; }

        [JsonPropertyName("Errors")]
        public object[]? Errors { get; set; }
    }
}
