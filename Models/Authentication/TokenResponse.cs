namespace AccreditValidation.Models.Authentication
{
    using System.Text.Json.Serialization;

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName(".issued")]
        public string? Issued { get; set; }

        [JsonPropertyName(".expires")]
        public string? Expires { get; set; }
    }
}
