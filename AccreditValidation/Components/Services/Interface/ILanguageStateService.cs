namespace AccreditValidation.Components.Services.Interface
{
    public interface ILanguageStateService
    {
        event Action? OnLanguageChanged;
        void NotifyLanguageChanged();
        string GetFlagEmoji(string countryCode);
    }
}
