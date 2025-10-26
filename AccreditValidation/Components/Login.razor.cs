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

        private List<LanguageModel> LanguageList { get; set; } = new List<LanguageModel>();

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

                await CheckSecureStorage();

                if (AuthService != null && await AuthService.GetIsAuthenticatedAsync())
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
            }
        }

        private async Task HandleLoginAsync()
        {
            AppState.ShowSpinner = true;

            try
            {
                if (!ValidateLoginFields())
                {
                    AppState.ShowSpinner = false;
                    return;
                }

                //Todo - remove, local testing
                userLoginModel.SiteName = "QASTAGINGV5-UAT";
                userLoginModel.Username = "QASTAGINGAPI";
                userLoginModel.Password = "EAS!dsaq123ew";
                userLoginModel.RememberMe = true;
                userLoginModel.SelectedLanguageCode = await SecureStorage.GetAsync("selectedLanguageCode") ?? "en-GB";

                if (userLoginModel.SiteName == "QASTAGINGV5-UAT")
                {
                    //userLoginModel.ServerUrl = ($"https://qastagingv5-api-uat.accredit-solutions.com");
                    userLoginModel.ServerUrl = "http://qq0ihfzhzl.loclx.io";
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

                AppState.ShowSpinner = false; ;
            }
            catch (Exception ex)
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["LoginFailedMessage"]);
                AppState.ShowSpinner = false; ;
            }
        }

        private bool ValidateLoginFields()
        {
            if (string.IsNullOrWhiteSpace(userLoginModel.SiteName))
            {
                AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["SiteNameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Username))
            {
                AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["UsernameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Password))
            {
                AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["PasswordRequired"]);
                return false;
            }

            if (string.IsNullOrEmpty(userLoginModel.ServerUrl))
            {
                userLoginModel.ServerUrl = ($"https://{userLoginModel.SiteName}{ConstantsName.BaseUrl}");
                userLoginModel.PhotoUrl = ($"https://{userLoginModel.SiteName}{ConstantsName.PhotoUrl}");
            }

            return true;
        }

        private async Task CheckSecureStorage()
        {
            userLoginModel.Username = await SecureStorage.GetAsync("username");
            userLoginModel.Password = await SecureStorage.GetAsync("password");
            userLoginModel.SiteName = await SecureStorage.GetAsync("siteName");
            userLoginModel.SelectedLanguageCode = await SecureStorage.GetAsync("selectedLanguageCode");
            userLoginModel.RememberMe = (await SecureStorage.GetAsync("rememberMe")) == "true";
            if (userLoginModel.RememberMe)
            {
                await HandleLoginAsync();
            }
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
                SecureStorage.Remove("rememberMe");
                SecureStorage.Remove("username");
                SecureStorage.Remove("password");
                SecureStorage.Remove("siteName");
                SecureStorage.Remove("selectedLanguageCode");
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

        private async void OnTestButtonClick()
        {
            var hasPermission = await NotificationService.RequestPermissionAsync();

            if (hasPermission)
            {
                await NotificationService.ShowNotificationAsync(
                    "Button Clicked!",
                    "You pressed the button at " + DateTime.Now.ToShortTimeString(),
                    id: 1);
            }
        }

        private async void OnTestButtonClick2()
        {
            NavigationManager.NavigateTo("/example", true);
        }
    }
}
