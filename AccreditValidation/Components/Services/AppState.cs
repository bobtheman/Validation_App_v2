using AccreditValidation.Components.Services.Interface;
using System;

namespace AccreditValidation.Components.Services
{
    public class AppState : IAppState
    {
        public event Action? OnChange;

        private string _selectedAreaIdentifier = string.Empty;
        public string SelectedAreaIdentifier
        {
            get => _selectedAreaIdentifier;
            set => SetProperty(ref _selectedAreaIdentifier, value);
        }

        private string _selectedDirectionIdentifier = string.Empty;
        public string SelectedDirectionIdentifier
        {
            get => _selectedDirectionIdentifier;
            set => SetProperty(ref _selectedDirectionIdentifier, value);
        }

        private string _totalScans = string.Empty;
        public string TotalScans
        {
            get => _totalScans;
            set => SetProperty(ref _totalScans, value);
        }

        private string _totalOfflineRecords = string.Empty;
        public string TotalOfflineRecords
        {
            get => _totalOfflineRecords;
            set => SetProperty(ref _totalOfflineRecords, value);
        }

        private string _lastOfflineSync = string.Empty;
        public string LastOfflineSync
        {
            get => _lastOfflineSync;
            set => SetProperty(ref _lastOfflineSync, value);
        }

        private string _networkStatus = string.Empty;
        public string NetworkStatus
        {
            get => _networkStatus;
            set => SetProperty(ref _networkStatus, value);
        }

        private string _selectedLanguageCode = string.Empty;
        public string SelectedLanguageCode
        {
            get => _selectedLanguageCode;
            set => SetProperty(ref _selectedLanguageCode, value);
        }

        private string _selectedInputOptionCode = string.Empty;
        public string SelectedInputOptionCode
        {
            get => _selectedInputOptionCode;
            set => SetProperty(ref _selectedInputOptionCode, value);
        }

        private string _customBackgroundClass = string.Empty;
        public string CustomBackgroundClass
        {
            get => _customBackgroundClass;
            set => SetProperty(ref _customBackgroundClass, value);
        }

        private bool _showSpinner = false;
        public bool ShowSpinner
        {
            get => _showSpinner;
            set => SetBoolProperty(ref _showSpinner, value);
        }

        private string _selectedPage = string.Empty;
        public string SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }

        private bool _useOfflineMode = false;
        public bool UseOfflineMode
        {
            get => _useOfflineMode;
            set => SetBoolProperty(ref _useOfflineMode, value);
        }

        private bool _showFilter = false;
        public bool ShowFilter
        {
            get => _showFilter;
            set => SetBoolProperty(ref _showFilter, value);
        }

        private void SetProperty(ref string field, string value)
        {
            if (field != value)
            {
                field = value;
                NotifyStateChanged();
            }
        }

        private void SetBoolProperty(ref bool field, bool value)
        {
            if (field != value)
            {
                field = value;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
