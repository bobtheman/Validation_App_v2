using AccreditValidation.Shared.Constants;

namespace AccreditValidation.Shared.Models.Authentication
{
    public class UserLoginModel
    {
        public string? Username { get; set; } = string.Empty;

        public string? Password { get; set; } = string.Empty;

        public string? SiteName { get; set; } = string.Empty;

        public string? ServerUrl { get; set; } = string.Empty;

        public string? SelectedLanguageCode { get; set; } = ConstantsName.EN;

        public string? PhotoUrl { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
