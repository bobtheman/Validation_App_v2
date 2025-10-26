namespace AccreditValidation.Components.Services.Interface
{
    public interface IAppState
    {
        event Action? OnChange;
        string SelectedAreaIdentifier { get; set; }
        string SelectedDirectionIdentifier { get; set; }
        string TotalScans { get; set; }
        string TotalOfflineRecords { get; set; }
        string LastOfflineSync { get; set; }
        string NetworkStatus { get; set; }
        string SelectedLanguageCode { get; set; }
        string SelectedInputOptionCode { get; set; }
        string CustomBackgroundClass { get; set; }
        bool ShowSpinner { get; set; }
        string SelectedPage { get; set; }
        bool UseOfflineMode { get; set; }
        bool ShowFilter { get; set; }
    }
}
