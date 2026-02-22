namespace AccreditValidation.Shared.Constants
{
    public static class SecureStorageKeys
    {
        // ── Authentication ────────────────────────────────────────────────────
        public const string Token = "token";
        public const string HasAuth = "hasAuth";
        public const string RememberMe = "rememberMe";
        public const string IsAuthenticated = "IsAuthenticated";

        // ── Credentials ───────────────────────────────────────────────────────
        public const string Username = "username";
        public const string Password = "password";

        // ── Server ────────────────────────────────────────────────────────────
        public const string SiteName = "siteName";
        public const string ServerUrl = "serverUrl";

        // ── Security ──────────────────────────────────────────────────────────
        public const string UseFingerPrint = "useFingerPrint";
        public const string UseNfc = "useNfc";

        // ── Preferences ───────────────────────────────────────────────────────
        public const string SelectedLanguageCode = "selectedLanguageCode";
        public const string SelectedInputOptionCode = "selectedInputOptionCode";

        // ── Profile ───────────────────────────────────────────────────────────
        public const string PhotoUrl = "photoUrl";
    }
}