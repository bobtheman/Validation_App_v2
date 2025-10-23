namespace AccreditValidation.Helper
{
    using AccreditValidation.Helper.Interface;
    using System.Globalization;

    public class LanguageHelper : ILanguageHelper
    {
        private string _defaultLanguageCode = "EN";

        public Task<string> CheckLangauge()
        {
            CultureInfo cultureInfo;

            if (SecureStorage.GetAsync("selectedLanguageCode").Result != null)
            {
                cultureInfo = new CultureInfo(SecureStorage.GetAsync("selectedLanguageCode").Result);
            }
            else
            {
                cultureInfo = new CultureInfo(_defaultLanguageCode);
            }

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Preferences.Set("language", true);
            return Task.FromResult(cultureInfo.TwoLetterISOLanguageName);
        }
    }
}
