namespace AccreditValidation.Components.Pages
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Models;
    using AccreditValidation.Requests.V2;
    using AccreditValidation.Responses;
    using AccreditValidation.Shared.Constants;
    using AccreditValidation.Shared.Services.AlertService;
    using CommunityToolkit.Maui.Views;
    using Honeywell.AIDC.CrossPlatform;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Maui.ApplicationModel;
    using Plugin.Maui.Audio;
    using System.Globalization;
    using System.Threading.Tasks;

    public partial class Validation : IDisposable
    {
        // ── Injected services ─────────────────────────────────────────────────

        [Inject] IAppState AppState { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private ILocalizationService LocalizationService { get; set; }
        [Inject] private IScannerCodeHelper ScannerCodeHelper { get; set; }
        [Inject] private IDevicePlaformHelper DevicePlaformHelper { get; set; }
        [Inject] private IAudioManager AudioManager { get; set; }
        [Inject] private IAreaService AreaService { get; set; }
        [Inject] private IDirectionService DirectionService { get; set; }
        [Inject] private IAlertService AlertService { get; set; }
        [Inject] private IConnectivityChecker ConnectivityChecker { get; set; }
        [Inject] private IRestDataService RestDataService { get; set; }
        [Inject] private IOfflineDataService OfflineDataService { get; set; }
        [Inject] private IFileService FileService { get; set; }

        /// <summary>
        /// Injected NFC service. Registered as a singleton in MauiProgram.cs.
        /// </summary>
        [Inject] private INfcService NfcService { get; set; }

        // ── State ─────────────────────────────────────────────────────────────

        private List<Area> AreaList { get; set; } = new();
        private List<ValidationDirection> DirectionList { get; set; } = new();
        private string SelectedDirectionCode { get; set; } = Enums.ValidationDirection.In.ToString();
        private bool UseManualInput { get; set; } = false;
        private bool ShowResult { get; set; } = false;
        private string PhotoUrl { get; set; } = string.Empty;
        private string OrganisationName { get; set; } = string.Empty;
        private string SubTypeName { get; set; } = string.Empty;
        private string Name { get; set; } = string.Empty;
        private string Direction { get; set; } = string.Empty;
        private string ValidationResultName { get; set; } = string.Empty;
        private bool ShowFilter { get; set; } = false;
        public static string SelectedScannerName { get; set; } = string.Empty;
        private BarcodeReader MSelectedReader { get; set; }
        private bool MSoftOneShotScanStarted = false;
        private bool IsHoneywellDevice = false;
        private bool UseDeviceCameraView = false;
        private string StatusMessage { get; set; } = string.Empty;
        private string BarcodeData { get; set; } = string.Empty;
        private bool ShowValidationImageDiv { get; set; } = false;
        private BadgeValidationRequest badgeValidationRequest = new BadgeValidationRequest();
        private ElementReference validation_holder_container;
        private string CustomBackgroundClass = string.Empty;
        private bool isFirstRender = true;

        // NFC state
        private bool IsNfcAvailable { get; set; } = false;
        private bool IsNfcListening { get; set; } = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
            AppState.SelectedPage = LocalizationService["Validation"];
            AppState.ShowSpinner = true;

            var currentCulture = LocalizationService.GetCulture();
            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = currentCulture;

            UseManualInput = await CheckManualInputOption();

            AreaList = await GetAreaList();
            DirectionList = await GetDirectionsList();

            AlertService.RegisterRefreshCallback(StateHasChanged);

            IsNfcAvailable = NfcService.IsAvailable && NfcService.IsEnabled;

            AppState.ShowSpinner = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                isFirstRender = true;
                ShowFilter = true;
                StateHasChanged();
            }

            IsHoneywellDevice = DevicePlaformHelper.HoneywellDevice();
            UseDeviceCameraView = !DevicePlaformHelper.HoneywellDevice();

            if (IsHoneywellDevice)
            {
                OpenBarcodeReader();
            }
        }

        // ── NFC ───────────────────────────────────────────────────────────────
        private void ToggleNfcListening()
        {
            if (IsNfcListening)
            {
                StopNfc();
            }
            else
            {
                StartNfc();
            }
        }

        private void StartNfc()
        {
            if (!NfcService.IsAvailable || !NfcService.IsEnabled)
            {
                StatusMessage = LocalizationService["NfcNotAvailable"];
                StateHasChanged();
                return;
            }

            NfcService.TagRead -= OnNfcTagRead;
            NfcService.TagError -= OnNfcTagError;
            NfcService.TagRead += OnNfcTagRead;
            NfcService.TagError += OnNfcTagError;

            NfcService.StartListening();
            IsNfcListening = true;
            StatusMessage = LocalizationService["NfcReady"];
            StateHasChanged();
        }

        private void StopNfc()
        {
            NfcService.TagRead -= OnNfcTagRead;
            NfcService.TagError -= OnNfcTagError;
            NfcService.StopListening();
            IsNfcListening = false;
            StatusMessage = string.Empty;
            StateHasChanged();
        }

        private async void OnNfcTagRead(object? sender, string payload)
        {
            StopNfc();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ValidateEntry(payload);
                StateHasChanged();
            });
        }

        private async void OnNfcTagError(object? sender, string error)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (error.Contains("disabled"))
                {
#if ANDROID
                    var intent = new Android.Content.Intent(
                        Android.Provider.Settings.ActionNfcSettings);
                    Android.App.Application.Context.StartActivity(intent);
#endif
                }
                else
                {
                    await AlertService.ShowErrorAlertAsync(
                        LocalizationService["NfcError"], error);
                }
                StateHasChanged();
            });
        }

        // ── Area / direction helpers ──────────────────────────────────────────

        private async Task<List<Area>> GetAreaList()
        {
            try
            {
                return await AreaService.GetAreaList(AppState.SelectedAreaIdentifier);
            }
            catch (Exception)
            {
                return new List<Area>();
            }
        }

        private async Task<List<ValidationDirection>> GetDirectionsList()
        {
            return await DirectionService.GetValidationDirectionList(SelectedDirectionCode);
        }

        private async void ToggleFilter()
        {
            ShowFilter = !ShowFilter;
            AppState.ShowFilter = ShowFilter;
            StateHasChanged();
        }

        private async Task Clear()
        {
            ResetFrom();
        }

        private async Task Validate() => await ValidateEntry(BarcodeData);

        private void ResetForm()
        {
            BarcodeData = string.Empty;
            ShowResult = false;
            OrganisationName = SubTypeName = Name = PhotoUrl = StatusMessage = string.Empty;
        }

        // ── Entry Validation ──────────────────────────────────────────────────

        private async Task ValidateEntry(string barcode)
        {
            AppState.ShowSpinner = true;

            ResetFrom();

            if (string.IsNullOrWhiteSpace(barcode))
            {
                AppState.ShowSpinner = false;
                return;
            }

            if (IsHoneywellDevice)
            {
                ShowFilter = false;
            }

            var selectedAreaIdentifier = AreaList.FirstOrDefault(a => a.IsSelected)?.Identifier ?? string.Empty;
            var selectedDirectionIdentifier = DirectionList.FirstOrDefault(d => d.IsSelected)?.Identifier ?? string.Empty;

            if (string.IsNullOrEmpty(selectedAreaIdentifier))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["InvalidArea"],
                    LocalizationService["PleaseEnteraValidArea"]);
            }

            if (string.IsNullOrEmpty(selectedDirectionIdentifier))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["InvalidDirection"],
                    LocalizationService["PleaseEnteraValidDirection"]);
            }

            badgeValidationRequest.Barcode = barcode;
            badgeValidationRequest.AreaIdentifier = selectedAreaIdentifier;
            badgeValidationRequest.DateTime = DateTime.Now;
            badgeValidationRequest.Direction = selectedDirectionIdentifier == Enums.ValidationDirection.In.ToString()
                ? Enums.ValidationDirection.In
                : Enums.ValidationDirection.Out;

            await ProcessRequest(badgeValidationRequest);

            AppState.ShowSpinner = false;
        }

        // ── Dropdowns ─────────────────────────────────────────────────────────

        private void OnAreaChanged(ChangeEventArgs e)
        {
            var selectedIdentifier = e?.Value?.ToString() ?? string.Empty;
            AppState.SelectedAreaIdentifier = selectedIdentifier;

            foreach (var area in AreaList)
                area.IsSelected = area.Identifier == selectedIdentifier;

            StateHasChanged();
        }

        private void OnSelectedDirectionChanged(ChangeEventArgs e)
        {
            var selectedIdentifier = e?.Value?.ToString() ?? string.Empty;
            AppState.SelectedDirectionIdentifier = selectedIdentifier;

            foreach (var direction in DirectionList)
                direction.IsSelected = direction.Identifier == selectedIdentifier;

            StateHasChanged();
        }

        // ── Honeywell Scanner ─────────────────────────────────────────────────

        private async void MBarcodeReader_BarcodeDataReady(object sender, BarcodeDataArgs e)
        {
            if (MSoftOneShotScanStarted)
            {
                await MSelectedReader.SoftwareTriggerAsync(false);
                MSoftOneShotScanStarted = false;
            }
        }

        public async void OpenBarcodeReader()
        {
            SelectedScannerName = await ScannerCodeHelper.GetReaderList();
            MSelectedReader = new BarcodeReader(SelectedScannerName);

            if (MSelectedReader != null)
            {
                MSelectedReader.BarcodeDataReady += MBarcodeReader_BarcodeDataReady;
                ScannerCodeHelper.OpenBarcodeReader(MSelectedReader);
            }
        }

        public void CloseBarcodeScanner()
        {
            if (MSelectedReader != null)
            {
                MSelectedReader.BarcodeDataReady -= MBarcodeReader_BarcodeDataReady;
                ScannerCodeHelper.CloseBarcodeScanner(MSelectedReader, MSoftOneShotScanStarted);
            }
        }

        private async void UpdateDetailItemWithScannedValue(string contents)
        {
            var trimmed = contents?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                await ValidateEntry(trimmed);
            }
        }

        // ── Device Camera (MAUI) ──────────────────────────────────────────────

        private async Task OpenNativeCameraPage()
        {
            try
            {
                var barcodeData = await Application.Current.MainPage.ShowPopupAsync(
                    new Xaml.DeviceCamera(AudioManager));

                if (string.IsNullOrEmpty(barcodeData?.ToString()))
                {
                    await AlertService.ShowErrorAlertAsync(
                        LocalizationService["BadgeNotFound"],
                        LocalizationService["NoBarcodeDetected"]);
                    StateHasChanged();
                    return;
                }

                await ValidateEntry(barcodeData.ToString());
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening camera popup: {ex.Message}");
            }
        }

        // ── Validation response processing ────────────────────────────────────

        private async Task ProcessRequest(BadgeValidationRequest badgeValidationRequest)
        {
            var response = new BadgeValidationResponse();

            if (ConnectivityChecker.ConnectivityCheck())
                response = await RestDataService.ValidateRequest(badgeValidationRequest);
            else
                response = await OfflineDataService.ValidateRequestOffline(badgeValidationRequest);

            if (response != null && response.ValidationResultName != ConstantsName.Success && response.Badge != null)
            {
                SetDangerBackground();
                await SetResults(response);
                return;
            }

            if (response?.Badge == null)
            {
                SetDangerBackground();
                PhotoUrl = ConstantsName.DefaultImage;
                OrganisationName = LocalizationService["Error"];
                SubTypeName = LocalizationService["InvalidBarcode"];
                Name = LocalizationService["BadgeNotFound"];
                Direction = $"{LocalizationService["Direction"]} : {DirectionList.FirstOrDefault(d => d.IsSelected)?.Direction ?? string.Empty}";
                ValidationResultName = SetLocalizedValidationResultName(response?.ValidationResultName);
                ShowResult = true;
                ShowFilter = true;
                StateHasChanged();
                return;
            }

            SetSuccessBackground();
            await SetResults(response);
        }

        private async Task SetResults(BadgeValidationResponse response)
        {
            ShowResult = true;

            if (!string.IsNullOrEmpty(response.Badge.Photo))
                PhotoUrl = response.Badge.Photo;

            if (!string.IsNullOrEmpty(response.Badge.PhotoUrl) &&
                string.IsNullOrEmpty(PhotoUrl) &&
                response.Badge.PhotoDownloaded)
            {
                try
                {
                    PhotoUrl = await FileService.GetImageBaseString(response.Badge.PhotoUrl);
                }
                catch
                {
                    await AlertService.ShowErrorAlertAsync(
                        LocalizationService["AnErrorOccured"],
                        LocalizationService["PleaseTryAgain"]);
                }
            }

            ShowValidationImageDiv = !string.IsNullOrEmpty(PhotoUrl);

            OrganisationName = response.Badge?.ResponsibleOrganisationName ?? string.Empty;
            SubTypeName = response.Badge?.RegistrationSubTypeName ?? string.Empty;
            Name = $"{response.Badge?.Forename} {response.Badge?.Surname}";
            Direction = $"{LocalizationService["Direction"]} : {DirectionList.FirstOrDefault(d => d.IsSelected)?.Direction ?? string.Empty}";
            ValidationResultName = SetLocalizedValidationResultName(response.ValidationResultName);

            StateHasChanged();
        }

        private string SetLocalizedValidationResultName(string? validationResultName)
        {
            if (string.IsNullOrWhiteSpace(validationResultName))
                return string.Empty;

            try
            {
                var localized = LocalizationService[validationResultName];

                if (string.IsNullOrEmpty(localized))
                    return string.Empty;

                if (string.Equals(localized, validationResultName, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                return localized;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ResetFrom()
        {
            ShowResult = false;
            PhotoUrl = string.Empty;
            OrganisationName = string.Empty;
            Name = string.Empty;
            BarcodeData = string.Empty;
            ClearBackground();
            StateHasChanged();
        }

        // ── Background helpers ────────────────────────────────────────────────

        private void SetSuccessBackground() => AppState.CustomBackgroundClass = ConstantsName.BGCustomSuccess;
        private void SetDangerBackground() => AppState.CustomBackgroundClass = ConstantsName.BGCustomDanger;
        private void ClearBackground() => AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
        private void ClearBarcodeData() => BarcodeData = string.Empty;

        private async Task<bool> CheckManualInputOption()
        {
            var manualInputValue = await SecureStorage.GetAsync("selectedInputOptionCode");

            if (string.IsNullOrEmpty(manualInputValue))
                return false;

            return manualInputValue == ConstantsName.ShowManualInputCode;
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            // Ensure NFC and scanner are stopped when the page is torn down.
            if (IsNfcListening)
                StopNfc();

            CloseBarcodeScanner();
        }
    }
}