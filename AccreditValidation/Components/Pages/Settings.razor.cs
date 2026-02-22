namespace AccreditValidation.Components.Pages
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Models;
    using AccreditValidation.Shared.Constants;
    using AccreditValidation.Shared.Services.AlertService;
    using Microsoft.AspNetCore.Components;
    using System;
    using System.Globalization;

    public partial class Settings
    {
        // ── Injected services ─────────────────────────────────────────────────

        [Inject] private IAppState AppState { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private ILocalizationService LocalizationService { get; set; }
        [Inject] private ILanguageStateService LanguageStateService { get; set; }
        [Inject] private IVersionProvider VersionProvider { get; set; }
        [Inject] private IAlertService AlertService { get; set; }
        [Inject] private IDevicePlaformHelper DevicePlaformHelper { get; set; }

        [Inject] private INfcService NfcService { get; set; }

        // ── Fingerprint toggle ────────────────────────────────────────────────

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

        // ── NFC toggle ────────────────────────────────────────────────────────

        private bool _useNfc;

        public bool UseNfc
        {
            get => _useNfc;
            set
            {
                if (_useNfc != value)
                {
                    _useNfc = value;
                    ToggleNfc(value);
                }
            }
        }

        // ── State ─────────────────────────────────────────────────────────────

        protected string SelectedLanguageCode { get; set; }
        protected string SelectedInputOptionCode { get; set; } = ConstantsName.HideManualInputCode;

        private string AppVersion;
        private string Version;
        private string Build;

        private List<LanguageModel> LanguageList { get; set; } = new List<LanguageModel>();
        private List<InputOptionModel> InputOptionList { get; set; } = new List<InputOptionModel>();

        private bool ShowFingerPrintOption { get; set; } = false;

        private bool ShowNfcOption { get; set; } = false;

        private bool showConfirmResetApplicationDialog = false;
        private bool showConfirmLogoutDialog = false;
        private TaskCompletionSource<bool>? confirmResetApplication;
        private TaskCompletionSource<bool>? confirmLogout;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
            AppState.SelectedPage = LocalizationService["Settings"];

            LanguageList = await GetLanguageList();
            InputOptionList = await GetInputOptionList();

            // Restore persisted toggle states from SecureStorage
            _useFingerprint = await SecureStorage.GetAsync(SecureStorageKeys.UseFingerPrint) == "true";

            // NFC: default to enabled on first run when hardware is available
            var savedNfc = await SecureStorage.GetAsync(SecureStorageKeys.UseNfc);
            if (savedNfc == null && NfcService.IsAvailable)
            {
                _useNfc = true;
                await SecureStorage.SetAsync(SecureStorageKeys.UseNfc, "true");
            }
            else
            {
                _useNfc = savedNfc == "true";
            }

            AppVersion = await VersionProvider.GetVersionAsync();
            Version = VersionProvider.Version;
            Build = VersionProvider.Build;

            AlertService.RegisterRefreshCallback(StateHasChanged);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender)
                return;

            // Fingerprint: available on non-Honeywell devices
            ShowFingerPrintOption = !DevicePlaformHelper.HoneywellDevice();

            // NFC: available when hardware is present and the OS reports it enabled
            ShowNfcOption = NfcService.IsAvailable;

            StateHasChanged();
        }

        // ── Language ──────────────────────────────────────────────────────────

        private async Task<List<LanguageModel>> GetLanguageList()
        {
            var languages = LocalizationService.GetLanguageList();
            var savedCode = await SecureStorage.GetAsync(SecureStorageKeys.SelectedLanguageCode);

            foreach (var language in languages)
            {
                language.IsSelected = language.LanguageCode == savedCode;
                if (language.IsSelected)
                    SelectedLanguageCode = language.LanguageCode ?? LocalizationService.GetDefaultLanguageCode();
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
                await SecureStorage.SetAsync(SecureStorageKeys.SelectedLanguageCode, SelectedLanguageCode ?? LocalizationService.GetDefaultLanguageCode());
                AppState.SelectedLanguageCode = SelectedLanguageCode ?? LocalizationService.GetDefaultLanguageCode();
            }

            LanguageList = await GetLanguageList();
            InputOptionList = await GetInputOptionList();
            LanguageStateService.NotifyLanguageChanged();

            StateHasChanged();
        }

        // ── Input options ─────────────────────────────────────────────────────

        private async Task<List<InputOptionModel>> GetInputOptionList()
        {
            var selectedCode = await SecureStorage.GetAsync(SecureStorageKeys.SelectedInputOptionCode) ?? ConstantsName.HideManualInputCode;

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
                    await SecureStorage.SetAsync(SecureStorageKeys.SelectedInputOptionCode, option.InputOptionCode);
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
                await SecureStorage.SetAsync(SecureStorageKeys.SelectedInputOptionCode, selectedInputOption.InputOptionCode ?? ConstantsName.HideManualInputCode);
                AppState.SelectedInputOptionCode = selectedInputOption.InputOptionCode;
            }

            StateHasChanged();
        }

        // ── Toggle helpers ────────────────────────────────────────────────────

        private async void ToggleFingerprint(bool isEnabled)
        {
            await SecureStorage.SetAsync(SecureStorageKeys.UseFingerPrint, isEnabled ? "true" : "false");
        }

        private async void ToggleNfc(bool isEnabled)
        {
            await SecureStorage.SetAsync(SecureStorageKeys.UseNfc, isEnabled ? "true" : "false");
        }

        // ── External links ────────────────────────────────────────────────────

        private async Task Home()
        {
            try
            {
                var siteName = (await SecureStorage.GetAsync(SecureStorageKeys.SiteName))?.Replace("/", "") ?? string.Empty;
                Uri uri = new($"https://{siteName}{ConstantsName.SiteUrl}");
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    LocalizationService["Error"].ToString(),
                    ex.ToString(),
                    LocalizationService["Cancel"].ToString());
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
                await Application.Current.MainPage.DisplayAlert(
                    LocalizationService["Error"].ToString(),
                    ex.ToString(),
                    LocalizationService["Cancel"].ToString());
            }
        }

        // ── Logout ────────────────────────────────────────────────────────────

        private async Task Logout()
        {
            if (!await ShowConfirmLogoutDialog())
                return;

            AuthService.Logout();
            NavigationManager.NavigateTo("/", forceLoad: true);
            SecureStorage.Remove(SecureStorageKeys.RememberMe);
            SecureStorage.Remove(SecureStorageKeys.Token);
            SecureStorage.Remove(SecureStorageKeys.HasAuth);
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

        // ── Reset application ─────────────────────────────────────────────────

        private async Task ResetApplication()
        {
            if (!await ShowConfirmResetApplicationDialog())
                return;

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
            SecureStorage.Remove(SecureStorageKeys.Username);
            SecureStorage.Remove(SecureStorageKeys.Password);
            SecureStorage.Remove(SecureStorageKeys.SiteName);
            SecureStorage.Remove(SecureStorageKeys.RememberMe);
            SecureStorage.Remove(SecureStorageKeys.UseFingerPrint);
            SecureStorage.Remove(SecureStorageKeys.UseNfc);
            SecureStorage.Remove(SecureStorageKeys.PhotoUrl);
            SecureStorage.Remove(SecureStorageKeys.SelectedLanguageCode);
            SecureStorage.Remove(SecureStorageKeys.SelectedInputOptionCode);
            SecureStorage.Remove(SecureStorageKeys.ServerUrl);
            SecureStorage.Remove(SecureStorageKeys.Token);
            SecureStorage.Remove(SecureStorageKeys.HasAuth);
            Application.Current.Quit();
        }
    }
}