namespace AccreditValidation.Components.Pages
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Models;
    using AccreditValidation.Resources.Constants;
    using Microsoft.AspNetCore.Components;
    using System;
    using System.Globalization;

    public partial class Settings
    {
        private bool _useFingerprint;
        public bool UseFingerprint
        {
            get => _useFingerprint;
            set
            {
                if (_useFingerprint != value)
                {
                    _useFingerprint = value;
                    ToggleFingerprint(value);
                }
            }
        }

        [Inject] IAppState AppState { get; set; }
        protected string SelectedLanguageCode { get; set; }

        protected string SelectedInputOptionCode { get; set; } = ConstantsName.HideManualInputCode;

        private string AppVersion;
        private string Version;
        private string Build;
        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IAuthService AuthService { get; set; }

        [Inject] ILocalizationService LocalizationService { get; set; }

        [Inject] private ILanguageStateService LanguageStateService { get; set; }

        [Inject] private IVersionProvider VersionProvider { get; set; }

        [Inject] private IAlertService AlertService { get; set; }

        [Inject] private IDevicePlaformHelper DevicePlaformHelper { get; set; }

        private List<LanguageModel> LanguageList { get; set; } = new List<LanguageModel>();

        private List<InputOptionModel> InputOptionList { get; set; } = new List<InputOptionModel>();
        private bool ShowFingerPrintOption { get; set; } = false;

        private bool showConfirmResetApplicationDialog = false;

        private bool showConfirmLogoutDialog = false;

        private TaskCompletionSource<bool>? confirmResetApplication;

        private TaskCompletionSource<bool>? confirmLogout;

        protected override async Task OnInitializedAsync()
        {
            AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
            AppState.SelectedPage = LocalizationService["Settings"];
            LanguageList = await GetLanguageList();
            InputOptionList = await GetInputOptionList();
            UseFingerprint = await SecureStorage.GetAsync("useFingerPrint") == "true";
            AppVersion = await VersionProvider.GetVersionAsync();
            Version = VersionProvider.Version;
            Build = VersionProvider.Build;
            AlertService.RegisterRefreshCallback(StateHasChanged); 
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
            }

            if (!DevicePlaformHelper.HoneywellDevice())
            {
                ShowFingerPrintOption = true;
                
            }

            StateHasChanged();
        }

        private async Task<List<LanguageModel>> GetLanguageList()
        {
            var languages = LocalizationService.GetLanguageList();

            var setSelectedLanguage = await SecureStorage.GetAsync("selectedLanguageCode");

            foreach (var language in languages)
            {
                language.IsSelected = language.LanguageCode == setSelectedLanguage;
                if (language.IsSelected)
                {
                    SelectedLanguageCode = language.LanguageCode ?? LocalizationService.GetDefaultLanguageCode();
                }
            }

            return languages;
        }

        private async Task OnLanguageChanged(ChangeEventArgs e)
        {
            SelectedLanguageCode = e.Value?.ToString();

            var selectedLang = LanguageList.FirstOrDefault(l => l.LanguageCode == SelectedLanguageCode);

            if (selectedLang != null)
            {
                LocalizationService.SetCulture(new CultureInfo(selectedLang.LanguageCode ?? LocalizationService.GetDefaultLanguageCode()));
                await SecureStorage.SetAsync("selectedLanguageCode", SelectedLanguageCode ?? LocalizationService.GetDefaultLanguageCode());
                AppState.SelectedLanguageCode = SelectedLanguageCode ?? LocalizationService.GetDefaultLanguageCode();
            }

            LanguageList = await GetLanguageList();

            InputOptionList = await GetInputOptionList();

            LanguageStateService.NotifyLanguageChanged();

            StateHasChanged();
        }

        private async Task<List<InputOptionModel>> GetInputOptionList()
        {
            var selectedCode = await SecureStorage.GetAsync("selectedInputOptionCode") ?? ConstantsName.HideManualInputCode;

            var inputOptionList = new List<InputOptionModel>
            {
                new InputOptionModel { InputOptionName = LocalizationService["HideManualInput"], InputOptionCode = ConstantsName.HideManualInputCode },
                new InputOptionModel { InputOptionName = LocalizationService["ShowManualInput"], InputOptionCode = ConstantsName.ShowManualInputCode }
            };

            foreach (var option in inputOptionList)
            {
                option.IsSelected = option.InputOptionCode == selectedCode;
                if (option.IsSelected)
                {
                    await SecureStorage.SetAsync("selectedInputOptionCode", option.InputOptionCode);
                    AppState.SelectedInputOptionCode = option.InputOptionCode;
                }
            }

            return inputOptionList;
        }

        private async Task OnInputOptionChanged(ChangeEventArgs e)
        {
            SelectedInputOptionCode = e.Value?.ToString();

            var selectedInputOption = InputOptionList.FirstOrDefault(x => x.InputOptionCode == SelectedInputOptionCode);

            if (selectedInputOption != null)
            {
                await SecureStorage.SetAsync("selectedInputOptionCode", selectedInputOption.InputOptionCode ?? ConstantsName.HideManualInputCode);
                AppState.SelectedInputOptionCode = selectedInputOption.InputOptionCode;
            }

            StateHasChanged();
        }

        private async void ToggleFingerprint(bool isEnabled)
        {
            if (isEnabled)
            {
                await SecureStorage.SetAsync("useFingerPrint", "true");
            }
            else
            {
                await SecureStorage.SetAsync("useFingerPrint", "false");
            }
        }

        private async Task Home()
        {
            try
            {
                Uri uri = new($"https://{(await SecureStorage.GetAsync("siteName")).Replace("/", "")}{ConstantsName.SiteUrl}");
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(LocalizationService["Error"].ToString(), ex.ToString(), LocalizationService["Cancel"].ToString());
            }
        }

        private async Task Feedback()
        {
            try
            {
                Uri uri = new("https://accredit-solutions.zendesk.com");
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(LocalizationService["Error"].ToString(), ex.ToString(), LocalizationService["Cancel"].ToString());
            }
        }

        #region logout
        private async Task Logout()
        {
            if (!await ShowConfirmLogoutDialog())
            {
                return;
            }

            AuthService.Logout();
            NavigationManager.NavigateTo("/", forceLoad: true);
            SecureStorage.Remove("rememberMe");
            SecureStorage.Remove("token");
            SecureStorage.Remove("hasAuth");
        }

        private async Task<bool> ShowConfirmLogoutDialog()
        {
            confirmLogout = new TaskCompletionSource<bool>();
            showConfirmLogoutDialog = true;
            StateHasChanged();
            return await confirmLogout.Task;
        }

        private void ConfirmLogout(bool confirmed)
        {
            showConfirmLogoutDialog = false;
            confirmLogout?.SetResult(confirmed);
        }
        #endregion

        #region reset application
        private async Task ResetApplication()
        {
            if (!await ShowConfirmResetApplicationDialog())
            {
                return;
            }

            await ResetApp();
        }

        private async Task<bool> ShowConfirmResetApplicationDialog()
        {
            confirmResetApplication = new TaskCompletionSource<bool>();
            showConfirmResetApplicationDialog = true;
            StateHasChanged();
            return await confirmResetApplication.Task;
        }

        private void ConfirmResetApplication(bool confirmed)
        {
            showConfirmResetApplicationDialog = false;
            confirmResetApplication?.SetResult(confirmed);
        }

        private async Task ResetApp()
        {
            SecureStorage.Remove("username");
            SecureStorage.Remove("password");
            SecureStorage.Remove("siteName");
            SecureStorage.Remove("rememberMe");
            SecureStorage.Remove("useFingerPrint");
            SecureStorage.Remove("photoUrl");
            SecureStorage.Remove("selectedLanguageCode");
            SecureStorage.Remove("selectedInputOptionCode");
            SecureStorage.Remove("serverUrl");
            SecureStorage.Remove("token");
            SecureStorage.Remove("hasAuth");
            //await _offlineDataService.DeleteDatabaseAysnc();
            Application.Current.Quit();
        }
        #endregion
    }
}
