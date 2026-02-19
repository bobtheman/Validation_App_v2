namespace AccreditValidation.Components
{
    using AccreditValidation.Components.Services;
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Models;
    using AccreditValidation.Models.Error;
    using AccreditValidation.Shared.Constants;
    using AccreditValidation.Shared.Models.Authentication;
    using AccreditValidation.Shared.Services.AlertService;
    using AccreditValidation.Shared.Services.Notification;
    using Microsoft.AspNetCore.Components;
    using Plugin.Fingerprint.Abstractions;
    using System.Globalization;

    public partial class Login : ComponentBase
    {
        protected UserLoginModel userLoginModel = new UserLoginModel();

        protected ErrorModel error = new ErrorModel();

        private List<LanguageModel> LanguageList { get; set; } = [];

        protected string SelectedLanguageCode { get; set; } = "en-GB";
        [Inject] private IAppState AppState { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ILocalizationService LocalizationService { get; set; }
        [Inject] private IAlertService AlertService { get; set; }
        [Inject] private ILanguageStateService LanguageStateService { get; set; }
        [Inject] private IFingerprint Fingerprint { get; set; }
        [Inject] private INotificationService NotificationService { get; set; }

        private bool isSiteNameVisible = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AppState.ShowSpinner = true;
                var useFingerprint = await SecureStorage.GetAsync("useFingerPrint");

                if (useFingerprint == "true")
                {
                    await ProcessBiometricLogin();
                }

                var loginHandled = await CheckSecureStorage();

                // Only check authentication if login wasn't already handled
                if (!loginHandled && AuthService != null && await AuthService.GetIsAuthenticatedAsync())
                {
                    HandleLoginAsync();
                }

                LanguageList = GetLanguageList();
                AlertService.RegisterRefreshCallback(StateHasChanged);
                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                error.ErrorMessage = $"Initialization failed: {ex.Message}";
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], error.ErrorMessage);
                AppState.ShowSpinner = false;
            }
        }

        private async Task HandleLoginAsync()
        {
            AppState.ShowSpinner = true;

            try
            {
                if (!await ValidateLoginFieldsAsync())
                {
                    AppState.ShowSpinner = false;
                    return;
                }

                // Ensure language code has a default
                userLoginModel.SelectedLanguageCode ??= "en-GB";

#if DEBUG
                //Todo - remove, local testing
                userLoginModel.SiteName = "QASTAGINGV5-TEST";
                userLoginModel.Username = "qastagingapi";
                userLoginModel.Password = "EAS!dsaq123ew";
                userLoginModel.RememberMe = true;
#endif

                if (userLoginModel.SiteName == "QASTAGINGV5-TEST")
                {
                    userLoginModel.ServerUrl = "http://vk5x8cqusr.loclx.io";
                }

                var tokenResponse = await AuthService.AuthenticateUserAsync(userLoginModel);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {

                    await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["LoginFailedMessage"]);
                    AppState.ShowSpinner = false;
                    return;
                }

                await SetSiteParameters(userLoginModel, tokenResponse.AccessToken);

                NavigationManager.NavigateTo("/Validation", true);

                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["LoginFailedMessage"]);
                AppState.ShowSpinner = false; ;
            }
        }

        private async Task<bool> ValidateLoginFieldsAsync()
        {
            if (string.IsNullOrWhiteSpace(userLoginModel.SiteName))
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["SiteNameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Username))
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["UsernameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Password))
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["PasswordRequired"]);
                return false;
            }

            if (string.IsNullOrEmpty(userLoginModel.ServerUrl))
            {
                userLoginModel.ServerUrl = $"https://{userLoginModel.SiteName}{ConstantsName.BaseUrl}";
                userLoginModel.PhotoUrl = $"https://{userLoginModel.SiteName}{ConstantsName.PhotoUrl}";
            }

            return true;
        }

        private async Task<bool> CheckSecureStorage()
        {
            userLoginModel.Username = await SecureStorage.GetAsync("username");
            userLoginModel.Password = await SecureStorage.GetAsync("password");
            userLoginModel.SiteName = await SecureStorage.GetAsync("siteName");
            userLoginModel.SelectedLanguageCode = await SecureStorage.GetAsync("selectedLanguageCode");
            userLoginModel.RememberMe = (await SecureStorage.GetAsync("rememberMe")) == "true";
            if (userLoginModel.RememberMe)
            {
                await HandleLoginAsync();
                return true;
            }
            return false;
        }

        private async Task SetSiteParameters(UserLoginModel userLoginModel, string accessToken)
        {

            await SecureStorage.SetAsync("token", accessToken);
            await SecureStorage.SetAsync("serverUrl", userLoginModel.ServerUrl);

            if (userLoginModel.RememberMe)
            {
                await SecureStorage.SetAsync("username", userLoginModel.Username);
                await SecureStorage.SetAsync("password", userLoginModel.Password);
                await SecureStorage.SetAsync("siteName", userLoginModel.SiteName);
                await SecureStorage.SetAsync("selectedLanguageCode", userLoginModel.SelectedLanguageCode);
                await SecureStorage.SetAsync("rememberMe", "true");
            }
            else
            {
                var keysToRemove = new[] { "rememberMe", "username", "password", "siteName", "selectedLanguageCode" };
                foreach (var key in keysToRemove)
                {
                    SecureStorage.Remove(key);
                }
            }
        }

        private async Task ProcessBiometricLogin()
        {
            try
            {
                var useFingerprint = await SecureStorage.GetAsync("useFingerPrint");

                if (useFingerprint == "false")
                {
                    return;
                }

                var username = await SecureStorage.GetAsync("username");
                var password = await SecureStorage.GetAsync("password");
                var siteName = await SecureStorage.GetAsync("siteName");
                var selectedLanguageCode = await SecureStorage.GetAsync("selectedLanguageCode");
                var rememberMeValue = await SecureStorage.GetAsync("rememberMe");
                var rememberMe = rememberMeValue == null ? "false" : rememberMeValue;

                if (string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(siteName) ||
                    string.IsNullOrEmpty(rememberMe) ||
                    rememberMe == "false")
                {
                    return;
                }

                var request = new AuthenticationRequestConfiguration(
                    LocalizationService["Verify"],
                    LocalizationService["UseFingerprintMessage"]
                );

                var result = await Fingerprint.AuthenticateAsync(request);

                if (result.Authenticated)
                {
                    userLoginModel.Username = username;
                    userLoginModel.Password = password;
                    userLoginModel.SiteName = siteName;
                    userLoginModel.SelectedLanguageCode = selectedLanguageCode ?? LocalizationService.GetDefaultLanguageCode();
                    userLoginModel.RememberMe = true;

                    await HandleLoginAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Biometric Login] Error: {ex.Message}");
            }
        }

        private List<LanguageModel> GetLanguageList()
        {
            var languages = LocalizationService.GetLanguageList();

            foreach (var language in languages)
            {
                language.IsSelected = language.LanguageCode == LocalizationService.GetCulture().Name;
                if (language.IsSelected)
                {
                    SelectedLanguageCode = language.LanguageCode ?? LocalizationService.GetDefaultLanguageCode();
                }
            }

            return languages;
        }

        private void OnLanguageChanged(ChangeEventArgs e)
        {
            SelectedLanguageCode = e.Value?.ToString() ?? LocalizationService.GetDefaultLanguageCode();

            var selectedLang = LanguageList.FirstOrDefault(l => l.LanguageCode == SelectedLanguageCode);

            userLoginModel.SelectedLanguageCode = SelectedLanguageCode;

            if (selectedLang != null)
            {
                LocalizationService.SetCulture(new CultureInfo(selectedLang.LanguageCode ?? SelectedLanguageCode));
                StateHasChanged();
            }
        }

        private void ToggleSiteNameVisibility()
        {
            isSiteNameVisible = !isSiteNameVisible;
        }
    }
}
