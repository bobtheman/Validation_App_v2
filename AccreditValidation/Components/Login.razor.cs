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

        // ── Injected services ─────────────────────────────────────────────────

        [Inject] private IAppState AppState { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ILocalizationService LocalizationService { get; set; }
        [Inject] private IAlertService AlertService { get; set; }
        [Inject] private ILanguageStateService LanguageStateService { get; set; }
        [Inject] private IFingerprint Fingerprint { get; set; }
        [Inject] private INotificationService NotificationService { get; set; }

        private bool isSiteNameVisible = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AppState.ShowSpinner = true;

                var useFingerprintTask = SecureStorage.GetAsync(SecureStorageKeys.UseFingerPrint);
                var usernameTask = SecureStorage.GetAsync(SecureStorageKeys.Username);
                var passwordTask = SecureStorage.GetAsync(SecureStorageKeys.Password);
                var siteNameTask = SecureStorage.GetAsync(SecureStorageKeys.SiteName);
                var languageCodeTask = SecureStorage.GetAsync(SecureStorageKeys.SelectedLanguageCode);
                var rememberMeTask = SecureStorage.GetAsync(SecureStorageKeys.RememberMe);
                var isAuthenticatedTask = AuthService.GetIsAuthenticatedAsync();

                await Task.WhenAll(
                    useFingerprintTask,
                    usernameTask,
                    passwordTask,
                    siteNameTask,
                    languageCodeTask,
                    rememberMeTask,
                    (Task)isAuthenticatedTask);

                var useFingerprint = useFingerprintTask.Result == "true";
                var username = usernameTask.Result;
                var password = passwordTask.Result;
                var siteName = siteNameTask.Result;
                var languageCode = languageCodeTask.Result;
                var rememberMe = rememberMeTask.Result == "true";
                var isAuthenticated = isAuthenticatedTask.Result;

                AlertService.RegisterRefreshCallback(StateHasChanged);
                LanguageList = GetLanguageList();

                if (useFingerprint)
                {
                    var biometricHandled = await ProcessBiometricLogin(
                        username, password, siteName, languageCode, rememberMe);

                    if (biometricHandled)
                    {
                        AppState.ShowSpinner = false;
                        return;
                    }
                }

                // ── Remember-me auto-login
                if (rememberMe)
                {
                    PopulateModelFromStorage(username, password, siteName, languageCode, rememberMe);
                    await HandleLoginAsync();
                    AppState.ShowSpinner = false;
                    return;
                }

                // ── Fallback: session was already authenticated (e.g. app resumed)
                if (isAuthenticated)
                {
                    await HandleLoginAsync();
                }

                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                error.ErrorMessage = $"Initialization failed: {ex.Message}";
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], error.ErrorMessage);
                AppState.ShowSpinner = false;
            }
        }

        // ── Login ─────────────────────────────────────────────────────────────

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

                userLoginModel.SelectedLanguageCode ??= "en-GB";

#if DEBUG
                userLoginModel.SiteName = "QATEST2024";
                userLoginModel.Username = "qastagingapi";
                userLoginModel.Password = "EAS!dsaq123ew";
                userLoginModel.RememberMe = true;
#endif

                if (userLoginModel.SiteName == "QATEST2024")
                {
                    //userLoginModel.ServerUrl = "http://ym3qvlcho4.loclx.io";
                    userLoginModel.ServerUrl = "https://qatest2024-api.accredit-solutions.com";
                }

                var tokenResponse = await AuthService.AuthenticateUserAsync(userLoginModel);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    await AlertService.ShowErrorAlertAsync(
                        LocalizationService["Error"],
                        LocalizationService["LoginFailedMessage"]);
                    AppState.ShowSpinner = false;
                    return;
                }

                await SetSiteParameters(userLoginModel, tokenResponse.AccessToken);

                NavigationManager.NavigateTo("/Validation", true);

                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["Error"],
                    LocalizationService["LoginFailedMessage"]);
                AppState.ShowSpinner = false;
            }
        }

        // ── Validation ────────────────────────────────────────────────────────

        private async Task<bool> ValidateLoginFieldsAsync()
        {
            if (string.IsNullOrWhiteSpace(userLoginModel.SiteName))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["Error"],
                    LocalizationService["SiteNameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Username))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["Error"],
                    LocalizationService["UsernameRequired"]);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userLoginModel.Password))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["Error"],
                    LocalizationService["PasswordRequired"]);
                return false;
            }

            if (string.IsNullOrEmpty(userLoginModel.ServerUrl))
            {
                userLoginModel.ServerUrl = $"https://{userLoginModel.SiteName}{ConstantsName.BaseUrl}";
                userLoginModel.PhotoUrl = $"https://{userLoginModel.SiteName}{ConstantsName.PhotoUrl}";
            }

            return true;
        }

        // ── SecureStorage write ───────────────────────────────────────────────

        private async Task SetSiteParameters(UserLoginModel model, string accessToken)
        {
            // Always save credentials if fingerprint is enabled — biometric needs them on next launch
            var saveCredentials = model.RememberMe ||
                                  await SecureStorage.GetAsync(SecureStorageKeys.UseFingerPrint) == "true";

            if (saveCredentials)
            {
                await Task.WhenAll(
                    SecureStorage.SetAsync(SecureStorageKeys.Token, accessToken),
                    SecureStorage.SetAsync(SecureStorageKeys.ServerUrl, model.ServerUrl),
                    SecureStorage.SetAsync(SecureStorageKeys.Username, model.Username),
                    SecureStorage.SetAsync(SecureStorageKeys.Password, model.Password),
                    SecureStorage.SetAsync(SecureStorageKeys.SiteName, model.SiteName),
                    SecureStorage.SetAsync(SecureStorageKeys.SelectedLanguageCode, model.SelectedLanguageCode)
                );

                if (model.RememberMe)
                    await SecureStorage.SetAsync(SecureStorageKeys.RememberMe, "true");
            }
            else
            {
                // No RememberMe and no fingerprint — save only what's needed for this session
                await Task.WhenAll(
                    SecureStorage.SetAsync(SecureStorageKeys.Token, accessToken),
                    SecureStorage.SetAsync(SecureStorageKeys.ServerUrl, model.ServerUrl)
                );

                SecureStorage.Remove(SecureStorageKeys.RememberMe);
                SecureStorage.Remove(SecureStorageKeys.Username);
                SecureStorage.Remove(SecureStorageKeys.Password);
                SecureStorage.Remove(SecureStorageKeys.SiteName);
                SecureStorage.Remove(SecureStorageKeys.SelectedLanguageCode);
            }
        }

        // ── Biometric login ───────────────────────────────────────────────────
        private async Task<bool> ProcessBiometricLogin(
            string? username,
            string? password,
            string? siteName,
            string? languageCode,
            bool rememberMe)
        {
            try
            {
                // Require saved credentials — biometric is independent of RememberMe
                if (string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(siteName))
                {
                    return false;
                }

                var request = new AuthenticationRequestConfiguration(
                    LocalizationService["Verify"],
                    LocalizationService["UseFingerprintMessage"]
                );

                var result = await Fingerprint.AuthenticateAsync(request);

                if (!result.Authenticated)
                    return false;

                PopulateModelFromStorage(username, password, siteName, languageCode, rememberMe);
                await HandleLoginAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Biometric Login] Error: {ex.Message}");
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PopulateModelFromStorage(
            string? username,
            string? password,
            string? siteName,
            string? languageCode,
            bool rememberMe)
        {
            userLoginModel.Username = username;
            userLoginModel.Password = password;
            userLoginModel.SiteName = siteName;
            userLoginModel.SelectedLanguageCode = languageCode ?? LocalizationService.GetDefaultLanguageCode();
            userLoginModel.RememberMe = rememberMe;
        }

        private List<LanguageModel> GetLanguageList()
        {
            var languages = LocalizationService.GetLanguageList();

            foreach (var language in languages)
            {
                language.IsSelected = language.LanguageCode == LocalizationService.GetCulture().Name;
                if (language.IsSelected)
                    SelectedLanguageCode = language.LanguageCode ?? LocalizationService.GetDefaultLanguageCode();
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