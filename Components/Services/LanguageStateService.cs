using AccreditValidation.Components.Services.Interface;
using AccreditValidation.Resources.Constants;

namespace AccreditValidation.Components.Services
{
    public class LanguageStateService : ILanguageStateService
    {
        public event Action? OnLanguageChanged;

        public void NotifyLanguageChanged()
        {
            OnLanguageChanged?.Invoke();
        }

        public string GetFlagEmoji(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return string.Empty;
            }

            if (countryCode == ConstantsName.EN)
            {
                countryCode = ConstantsName.GB;
            }

            return string.Concat(
                countryCode.Substring(0, 2).ToUpper().Select(c => char.ConvertFromUtf32(c + (int)Enums.RegionalIndicator.RegionalIndicatorSymbol))
            );
        }
    }

}
