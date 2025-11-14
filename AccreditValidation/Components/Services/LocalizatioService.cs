namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using global::AccreditValidation.Resources.Strings;
    using global::AccreditValidation.Shared.Constants;
    using System.ComponentModel;
    using System.Globalization;

    public class LocalizationService : ILocalizationService, INotifyPropertyChanged
    {
        static LocalizationService()
        {
            AppResources.Culture = new CultureInfo(ConstantsName.EN);
        }

        public LocalizationService()
        {
            AppResources.Culture ??= new CultureInfo(ConstantsName.EN);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
        {
            get
            {
                try
                {
                    return AppResources.ResourceManager.GetString(key, AppResources.Culture) ?? "N/A";
                }
                catch
                {
                    return "N/A";
                }
            }
        }

        public CultureInfo GetCulture()
        {
            return AppResources.Culture;
        }

        public void SetCulture(CultureInfo culture)
        {
            AppResources.Culture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public string GetDefaultLanguageCode()
        {
            return ConstantsName.EN;
        }

        public List<LanguageModel> GetLanguageList()
        {
            return new List<LanguageModel>
            {
                new LanguageModel { LanguageName = this["English"], LanguageCode = ConstantsName.EN },
                new LanguageModel { LanguageName = this["German"], LanguageCode = ConstantsName.DE }
            };
        }

        public async Task<string> SetLocalizedValidationResultName(string? validationResultName)
        {
            if (string.IsNullOrWhiteSpace(validationResultName))
            {
                return string.Empty;
            }

            try
            {
                var localized = this[validationResultName];

                if (string.IsNullOrEmpty(localized))
                {
                    return string.Empty;
                }

                if (!string.Equals(localized, validationResultName, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                return localized;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
    }
}
