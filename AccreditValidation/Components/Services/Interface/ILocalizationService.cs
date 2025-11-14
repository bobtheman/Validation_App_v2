namespace AccreditValidation.Components.Services.Interface
{
    using global::AccreditValidation.Models;
    using System.ComponentModel;
    using System.Globalization;

    public interface ILocalizationService : INotifyPropertyChanged
    {
        string this[string key] { get; }

        CultureInfo GetCulture();

        void SetCulture(CultureInfo culture);

        string GetDefaultLanguageCode();

        List<LanguageModel> GetLanguageList();

        Task<string> SetLocalizedValidationResultName(string? validationResultName);
    }
}
