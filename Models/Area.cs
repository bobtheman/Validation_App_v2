namespace AccreditValidation.Models
{
    using System.Text.Json.Serialization;
    using SQLite;

    public class Area
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Identifier")]
        public string? Identifier { get; set; }

        public bool IsSelected { get; set; } = false;
    }
}
