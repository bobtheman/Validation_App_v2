namespace AccreditValidation.Shared.Constants
{
    public static class ConstantsName
    {
        #region Site data
        public static string BaseUrl = "-api.accredit-solutions.com";
        public static string PhotoUrl = ".accredit-solutions.com/";
        public static string ForogtPasswordUrl = ".accredit-solutions.com/Account/Forgot";
        public static string SiteUrl = ".accredit-solutions.com";
        #endregion

        #region Results
        public static string Success = "Success";
        public static string Failure = "Failure";
        public static string AreaNotFound = "AreaNotFound";
        public static string AreaNotActive = "AreaNotActive";
        public static string AreaNoConfigurations = "AreaNoConfigurations";
        public static string SpaceFull = "SpaceFull";
        public static string BadgeNotFound = "BadgeNotFound";
        public static string NoPrivileges = "NoPrivileges";
        public static string RegistrationNotApproved = "RegistrationNotApproved";
        public static string BadgeDeactivated = "BadgeDeactivated";
        public static string RegistrationSubTypeNotAllowed = "RegistrationSubTypeNotAllowed";
        public static string RegistrationNotFoundAtEvent = "RegistrationNotFoundAtEvent";
        #endregion

        #region Scanner
        public static string DefaultReaderKey = "default";
        public static string Honeywell = "Honeywell";
        #endregion

        #region Langauge
        public static string EN = "en-GB";
        public static string GB = "GB";
        public static string DE = "de-DE";
        #endregion

        #region ManualInput
        public static string ShowManualInputCode = "Show";
        public static string HideManualInputCode = "Hide";
        #endregion

        #region ManualInput
        public static string Version = "1.0.0";
        #endregion

        #region AlertType
        public static string AlertSuccess = "success";
        public static string AlertError = "error";
        public static string AlertTypeSuccess = "alert-success";
        public static string AlertTypeDanger = "alert-danger";
        #endregion

        #region Background
        public static string BGCustomSuccess = "bg-custom-success";
        public static string BGCustomDanger = "bg-custom-danger";
        public static string BGCustomDefault = "bg-custom-clear";
        #endregion

        #region Network
        public static string Online = "Online";
        public static string Offline = "Offline";
        #endregion

        #region Endpoints   
        public static class Endpoints
        {
            public static string Validation = "/api/v3/accessControl";
            public static string Areas = "/api/v3/accessControl/areas";
            public static string File = "/api/v3/file/";
            public static string FileThumbnail = "/thumbnail";
            public static string ValidationResults = "/api/v3/accessControl/validationResults";
        }
        #endregion

        #region Headers
        public static class Headers
        {
            public static string Authorization = "Authorization";
            public static string Bearer = "Bearer";
            public static string ContentType = "Content-Type";
        }
        #endregion

        #region MimeTypes
        public static class MimeTypes
        {
            public static string ApplicationJson = "application/json";
        }
        #endregion

        #region SecureStorage
        public static string SecureStorageToken = "token";
        public static string SecureStorageServerUrl = "serverUrl";
        #endregion

        #region default image
        public static string DefaultImage = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciCiAgICAgdmlld0JveD0iMCAwIDY0IDY0IgogICAgIHdpZHRoPSI2NCIgaGVpZ2h0PSI2NCIKICAgICByb2xlPSJpbWciIGFyaWEtbGFiZWxsZWRieT0idGl0bGUtZGVzYyI+CiAgPHRpdGxlIGlkPSJ0aXRsZSI+Tm8gdXNlcjwvdGl0bGU+CiAgPGRlc2MgaWQ9ImRlc2MiPkNpcmN1bGFyIHBsYWNlaG9sZGVyIGF2YXRhciBzaG93aW5nIGEgZ2VuZXJpYyB1c2VyIHNpbGhvdWV0dGU8L2Rlc2M+CiAgPGNpcmNsZSBjeD0iMzIiIGN5PSIzMiIgcj0iMzEiIGZpbGw9IiNFNkU2RTYiLz4KICA8Y2lyY2xlIGN4PSIzMiIgY3k9IjIyIiByPSI5IiBmaWxsPSIjQjNCM0IzIi8+CiAgPHBhdGggZD0iTTEwIDUwYzAtOCA4LTE0IDIyLTE0czIyIDYgMjIgMTR2MkgxMHYtMnoiIGZpbGw9IiNCM0IzQjMiIC8+Cjwvc3ZnPg==";
        #endregion
    }
}
