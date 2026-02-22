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

    public partial class Validation : IDisposable
    {
        // ── Injected services ─────────────────────────────────────────────────

        [Inject] private IAppState AppState { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
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
        private string BarcodeData { get; set; } = string.Empty;
        private bool ShowValidationImageDiv { get; set; } = false;
        private string StatusMessage { get; set; } = string.Empty;
        private ElementReference validation_holder_container;

        // Device type — set once on first render
        private bool IsHoneywellDevice { get; set; } = false;
        private bool UseDeviceCameraView { get; set; } = false;

        // Honeywell scanner
        public static string SelectedScannerName { get; set; } = string.Empty;
        private BarcodeReader MSelectedReader { get; set; }
        private bool MSoftOneShotScanStarted = false;

        // NFC
        private bool IsNfcAvailable { get; set; } = false;
        private bool IsNfcListening { get; set; } = false;

        // Reused request object — avoids repeated allocations per scan
        private readonly BadgeValidationRequest _badgeValidationRequest = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
            AppState.SelectedPage = LocalizationService["Validation"];
            AppState.ShowSpinner = true;

            var currentCulture = LocalizationService.GetCulture();
            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = currentCulture;

            // Run independent data fetches in parallel
            var manualInputTask = CheckManualInputOption();
            var areaTask = GetAreaList();
            var directionTask = GetDirectionsList();
            var nfcEnabledTask = SecureStorage.GetAsync(SecureStorageKeys.UseNfc);

            await Task.WhenAll(manualInputTask, areaTask, directionTask, nfcEnabledTask);

            UseManualInput = manualInputTask.Result;
            AreaList = areaTask.Result;
            DirectionList = directionTask.Result;

            AlertService.RegisterRefreshCallback(StateHasChanged);

            // NFC: available only when hardware is present AND the user has enabled it in Settings
            var nfcUserEnabled = nfcEnabledTask.Result == "true";
            IsNfcAvailable = NfcService.IsAvailable && NfcService.IsEnabled && nfcUserEnabled;

            if (IsNfcAvailable)
                StartNfc();

            AppState.ShowSpinner = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            ShowFilter = true;

            IsHoneywellDevice = DevicePlaformHelper.HoneywellDevice();
            UseDeviceCameraView = !IsHoneywellDevice;

            if (IsHoneywellDevice)
                OpenBarcodeReader();

            StateHasChanged();
        }

        // ── NFC ───────────────────────────────────────────────────────────────
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
            // Stop listening immediately so a second tap does not fire before validation completes
            StopNfc();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ValidateEntry(payload);

                // Resume listening automatically after each successful read
                if (IsNfcAvailable)
                    StartNfc();

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

        private Task<List<ValidationDirection>> GetDirectionsList()
            => DirectionService.GetValidationDirectionList(SelectedDirectionCode);

        // ── Filter / UI helpers ───────────────────────────────────────────────

        private void ToggleFilter()
        {
            ShowFilter = !ShowFilter;
            AppState.ShowFilter = ShowFilter;
            StateHasChanged();
        }

        private Task Clear()
        {
            ResetFrom();
            return Task.CompletedTask;
        }

        private Task Validate() => ValidateEntry(BarcodeData);

        // ── Entry Validation ──────────────────────────────────────────────────

        private async Task ValidateEntry(string barcode)
        {
            AppState.ShowSpinner = true;
            ResetFrom();

            if (string.IsNullOrWhiteSpace(barcode))
            {
                AppState.ShowSpinner = false;
                StateHasChanged();
                return;
            }

            if (IsHoneywellDevice)
                ShowFilter = false;

            var selectedAreaIdentifier = AreaList.FirstOrDefault(a => a.IsSelected)?.Identifier ?? string.Empty;
            var selectedDirectionIdentifier = DirectionList.FirstOrDefault(d => d.IsSelected)?.Identifier ?? string.Empty;

            if (string.IsNullOrEmpty(selectedAreaIdentifier))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["InvalidArea"],
                    LocalizationService["PleaseEnteraValidArea"]);
                AppState.ShowSpinner = false;
                StateHasChanged();
                return;
            }

            if (string.IsNullOrEmpty(selectedDirectionIdentifier))
            {
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["InvalidDirection"],
                    LocalizationService["PleaseEnteraValidDirection"]);
                AppState.ShowSpinner = false;
                StateHasChanged();
                return;
            }

            _badgeValidationRequest.Barcode = barcode;
            _badgeValidationRequest.AreaIdentifier = selectedAreaIdentifier;
            _badgeValidationRequest.DateTime = DateTime.Now;
            _badgeValidationRequest.Direction = selectedDirectionIdentifier == Enums.ValidationDirection.In.ToString()
                ? Enums.ValidationDirection.In
                : Enums.ValidationDirection.Out;

            try
            {
                await ProcessRequest(_badgeValidationRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ValidateEntry] ProcessRequest failed: {ex.Message}");
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["AnErrorOccured"],
                    LocalizationService["PleaseTryAgain"]);
            }
            finally
            {
                AppState.ShowSpinner = false;
                StateHasChanged();
            }
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
            if (!string.IsNullOrEmpty(e.Data))      
            {
                await MainThread.InvokeOnMainThreadAsync(() => UpdateDetailItemWithScannedValue(e.Data));
            }

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
                await ValidateEntry(trimmed);
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
            var response = ConnectivityChecker.ConnectivityCheck()
                ? await RestDataService.ValidateRequest(badgeValidationRequest)
                : await OfflineDataService.ValidateRequestOffline(badgeValidationRequest);

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

        private async Task<bool> CheckManualInputOption()
        {
            var value = await SecureStorage.GetAsync(SecureStorageKeys.SelectedInputOptionCode);
            return !string.IsNullOrEmpty(value) && value == ConstantsName.ShowManualInputCode;
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (IsNfcListening)
                StopNfc();

            CloseBarcodeScanner();
        }
    }
}