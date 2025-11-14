namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;

    public partial class ContactImage
    {
        [JsonPropertyName("Id")]
        public Guid Id { get; set; }

        [JsonPropertyName("FileName")]
        public string FileName { get; set; }

        [JsonPropertyName("Base64EncodedImage")]
        public object Base64EncodedImage { get; set; }
    }
}
