namespace AccreditValidation.Components.Pages
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Models;
    using AccreditValidation.Requests.V3;
    using AccreditValidation.Shared.Constants;
    using AccreditValidation.Shared.Services.AlertService;
    using Microsoft.AspNetCore.Components;
    using System.Net;

    public partial class Data
    {
        [Inject] IAppState AppState { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private IAuthService AuthService { get; set; }

        [Inject] private ILocalizationService LocalizationService { get; set; }

        [Inject] private IConnectivityChecker ConnectivityChecker { get; set; }

        [Inject] private IRestDataService RestDataService { get; set; }

        [Inject] private IOfflineDataService OfflineDataService { get; set; }
        [Inject] private IAlertService AlertService { get; set; }

        private bool showConfirmDeleteDatabaseDialog = false;
        private bool showConfirmDownloadDataDialog = false;
        private bool showPasswordPrompt = false;
        private string passwordInput = string.Empty;
        private string alertMessage = string.Empty;
        private string alertType = string.Empty;
        private TaskCompletionSource<bool>? confirmDeleteDataBase;
        private TaskCompletionSource<string>? passwordTcs;
        private TaskCompletionSource<(bool confirm, bool downloadAllPhotos)>? confirmDownloadData;
        private bool showOfflineScanResultsModal = false;
        private string offlineScanResultsMessage = string.Empty;
        private List<OfflineScanData> OfflineScanRecords = new();
        private string barcodeFilter = string.Empty;
        private string resultFilter = "All";
        private ElementReference data_holder_container;
        private IEnumerable<OfflineScanData> FilteredRecords => OfflineScanRecords.Where(record =>
            (string.IsNullOrWhiteSpace(barcodeFilter) || record.Barcode.Contains(barcodeFilter, StringComparison.OrdinalIgnoreCase)) &&
            (resultFilter == "All" ||
             Enum.GetName(typeof(Enums.BadgeValidationResult), record.ValidationResult) == resultFilter)
        );

        protected override async Task OnInitializedAsync()
        {
            AppState.CustomBackgroundClass = ConstantsName.BGCustomDefault;
            AppState.NetworkStatus = ConnectivityChecker.ConnectivityCheck() ? ConstantsName.Online : ConstantsName.Offline;
            AppState.SelectedPage = LocalizationService["DataSync"];
            await CheckOfflineScans();
        }

        private async Task SyncData()
        {
            AppState.ShowSpinner = true;

            if (!ConnectivityChecker.ConnectivityCheck())
            {
                AppState.ShowSpinner = false;
                await ShowAlert(LocalizationService["Error"], LocalizationService["NetworkErrorMessage"], ConstantsName.Failure);
                return;
            }

            var offlineScaData = await OfflineDataService.GetOfflineScans();

            if (offlineScaData == null || !offlineScaData.Any())
            {
                AppState.ShowSpinner = false;
                await ShowAlert(LocalizationService["Error"], LocalizationService["NoOfflineScanRecords"], ConstantsName.Failure);
                return;
            }

            try
            {
                var result = await RestDataService.SyncValidationResults(offlineScaData.Select(x => new BadgeValidationRequest
                {
                    Barcode = x.Barcode,
                    AreaId = x.AreaId
                }));

                if (result == null)
                {
                    AppState.ShowSpinner = false;
                    await ShowAlert(LocalizationService["Error"], LocalizationService["SyncDataFailed"], ConstantsName.Failure);
                    return;
                }

                await OfflineDataService.DeleteAllOfflineRecordsAsync();
                await ShowAlert(LocalizationService["Success"], LocalizationService["SyncDataSuccess"], ConstantsName.Success);
                await CheckOfflineScans();
                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                AppState.ShowSpinner = false;
                await ShowAlert(LocalizationService["Error"], LocalizationService["SyncDataFailed"], ConstantsName.Failure);
                return;
            }
        }

        private async Task DeleteDatabase()
        {
            if (!await ShowConfirmDialog())
            {
                return;
            }

            var password = await ShowPasswordPrompt();
            if (!await ValidatePasswordAsync(password))
            {
                await ShowAlert(LocalizationService["Error"], LocalizationService["ValidPasswordRequired"], ConstantsName.Failure);
                return;
            }

            try
            {
                AppState.TotalScans = string.Empty;
                AppState.TotalOfflineRecords = string.Empty;
                AppState.LastOfflineSync = string.Empty;
                await OfflineDataService.DeleteAllOfflineRecordsAsync();

                await ShowAlert(LocalizationService["Success"], LocalizationService["DatabaseDeleted"], ConstantsName.Success);
            }
            catch (Exception ex)
            {
                await ShowAlert(LocalizationService["Error"], LocalizationService["DatabaseDeletedError"], ConstantsName.Failure);
            }
        }

        private async Task<bool> ShowConfirmDialog()
        {
            confirmDeleteDataBase = new TaskCompletionSource<bool>();
            showConfirmDeleteDatabaseDialog = true;
            StateHasChanged();
            return await confirmDeleteDataBase.Task;
        }

        private void ConfirmDelete(bool confirmed)
        {
            showConfirmDeleteDatabaseDialog = false;
            confirmDeleteDataBase?.SetResult(confirmed);
        }

        private async Task<(bool confirm, bool downloadAllPhotos)> ShowConfirmDownloadDialog()
        {
            confirmDownloadData = new TaskCompletionSource<(bool, bool)>();
            showConfirmDownloadDataDialog = true;
            StateHasChanged();
            return await confirmDownloadData.Task;
        }

        private void ConfirmDownloadData(bool confirmed, bool downloadAllPhotos)
        {
            showConfirmDownloadDataDialog = false;
            confirmDownloadData?.SetResult((confirmed, downloadAllPhotos));
        }

        // Password prompt
        private async Task<string> ShowPasswordPrompt()
        {
            passwordTcs = new TaskCompletionSource<string>();
            showPasswordPrompt = true;
            StateHasChanged();
            return await passwordTcs.Task;
        }

        private void ValidatePassword()
        {
            showPasswordPrompt = false;
            passwordTcs?.SetResult(passwordInput);
            passwordInput = string.Empty;
        }

        private async Task ShowAlert(string title, string message, string type)
        {
            alertMessage = $"{title}: {message}";
            alertType = type.ToLower();
            StateHasChanged();

            // Wait for 3 seconds before auto-clearing the alert
            await Task.Delay(3000);

            // Only clear if the user hasn't already dismissed it
            if (!string.IsNullOrEmpty(alertMessage))
            {
                alertMessage = string.Empty;
                StateHasChanged();
            }
        }

        private async Task<bool> ValidatePasswordAsync(string password)
        {
            if (password != await SecureStorage.GetAsync("password"))
            { 
                return false;
            }

            return true;
        }

        private void CancelPasswordPrompt()
        {
            showPasswordPrompt = false;
            passwordTcs?.SetResult(null);
            passwordInput = string.Empty;
        }

        private async Task DownloadData()
        {
            try
            {
                if (!ConnectivityChecker.ConnectivityCheck())
                {
                    await ShowAlert(LocalizationService["Error"], LocalizationService["NetworkConnectionRequired"], ConstantsName.Failure);
                    AppState.ShowSpinner = false;
                    return;
                }

                var (confirmed, downloadAllPhotos) = await ShowConfirmDownloadDialog();
                if (!confirmed)
                {
                    return;
                }

                AppState.ShowSpinner = true;

                var validationResultsResponse = await RestDataService.DownloadOfflineData(downloadAllPhotos);

                if (validationResultsResponse != null && validationResultsResponse.Data.Length > 0)
                {
                    await OfflineDataService.InitializeAsync();
                    await OfflineDataService.SetLocalValidationResultAsync(validationResultsResponse);
                    await OfflineDataService.SetOfflineAreaAsync();

                    AppState.TotalScans = "0";
                    AppState.TotalOfflineRecords = validationResultsResponse.TotalRecords.ToString();
                    AppState.LastOfflineSync = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    await ShowAlert(LocalizationService["Success"], LocalizationService["DownloadDataSuccessed"], ConstantsName.Success);
                }
                else
                {
                    await ShowAlert(LocalizationService["Error"], LocalizationService["DownloadDataFailed"], ConstantsName.Failure);
                }


                AppState.ShowSpinner = false;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during download: {ex.Message}");
                await ShowAlert(LocalizationService["Error"], LocalizationService["DownloadDataFailed"], ConstantsName.Failure);
                AppState.ShowSpinner = false;
                return;
            }
        }

        private async Task CheckOfflineScans()
        {
            try
            {
                AppState.ShowSpinner = true;

                var offlineScaData = await OfflineDataService.GetOfflineScans();

                if (offlineScaData != null && offlineScaData.Any())
                {
                    AppState.TotalOfflineRecords = offlineScaData.Count().ToString();
                    AppState.TotalScans = offlineScaData.Count(x => x.ScannedDateTime != null).ToString();
                }
                else
                {
                    AppState.TotalOfflineRecords = "0";
                    AppState.TotalScans = "0";
                }

                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                AppState.TotalOfflineRecords = "0";
                AppState.TotalScans = "0";

                AppState.ShowSpinner = false;
            }
        }

        private async Task LoadOfflineScans()
        {
            try
            {
                AppState.ShowSpinner = true;

                var offlineScanData = await OfflineDataService.GetOfflineScans();

                if (offlineScanData != null && offlineScanData.Any())
                {
                    var totalRecords = offlineScanData.Count();
                    var totalScanned = offlineScanData.Count(x => x.ScannedDateTime != null);

                    AppState.TotalOfflineRecords = totalRecords.ToString();
                    AppState.TotalScans = totalScanned.ToString();

                    offlineScanResultsMessage = $"{LocalizationService["TotalOfflineRecords"]}: {totalRecords}\n" +
                                                $"{LocalizationService["TotalScans"]}: {totalScanned}";

                    OfflineScanRecords = offlineScanData.ToList();
                }
                else
                {
                    OfflineScanRecords.Clear();
                    AppState.TotalOfflineRecords = "0";
                    AppState.TotalScans = "0";
                    offlineScanResultsMessage = LocalizationService["NoOfflineScanRecords"];
                }
            }
            catch (Exception ex)
            {
                OfflineScanRecords.Clear();
                AppState.TotalOfflineRecords = "0";
                AppState.TotalScans = "0";
            }

            showOfflineScanResultsModal = true;
            AppState.ShowSpinner = false;
            StateHasChanged();
        }

        private async Task DeleteOfflineRecord(OfflineScanData record)
        {
            bool confirmed = await AlertService.ShowConfirmAlertAsync(LocalizationService["DeleteScan"], LocalizationService["AreYouSure"], LocalizationService["OK"], LocalizationService["Cancel"]);

            if (!confirmed)
            {
                return;
            }

            AppState.ShowSpinner = true;

            try
            {
                await OfflineDataService.DeleteOfflineRecordAsync(record.Id);
                OfflineScanRecords.Remove(record);

                AppState.TotalOfflineRecords = OfflineScanRecords.Count.ToString();

                await AlertService.ShowSuccessAlertAsync(LocalizationService["Success"], LocalizationService["RecordDeleted"]);
            }
            catch
            {
                await AlertService.ShowErrorAlertAsync(LocalizationService["Error"], LocalizationService["RecordDeleteFailed"]);
            }

            AppState.ShowSpinner = false;

            StateHasChanged();
        }

        private async Task ToggleNetworkMode()
        {
            if (AppState.NetworkStatus == ConstantsName.Online)
            {
                ConnectivityChecker.ConnectivityCheck(true);
                AppState.NetworkStatus = ConstantsName.Offline;
            }
            else
            {
                AppState.UseOfflineMode = false;
                var isOnline = ConnectivityChecker.ConnectivityCheck();
                AppState.NetworkStatus = isOnline ? ConstantsName.Online : ConstantsName.Offline;
            }

            StateHasChanged();
        }

        private void CloseOfflineScanModal()
        {
            showOfflineScanResultsModal = false;
            StateHasChanged();
        }

        private async Task DeleteAllOfflineRecords()
        {
            try
            {
                AppState.ShowSpinner = true;

                bool confirmed = await AlertService.ShowConfirmAlertAsync(
                    LocalizationService["DeleteAllScanResults"],
                    LocalizationService["AreYouSure"],
                    LocalizationService["OK"],
                    LocalizationService["Cancel"]
                );

                if (confirmed)
                {
                    await OfflineDataService.DeleteAllOfflineRecordsAsync();
                    OfflineScanRecords.Clear();
                    AppState.TotalOfflineRecords = "0";
                    AppState.TotalScans = "0";
                    await AlertService.ShowSuccessAlertAsync(
                        LocalizationService["Success"],
                        LocalizationService["DeleteAllScanResultsDeleted"]
                    );
                }

                showOfflineScanResultsModal = false;
                StateHasChanged();
                AppState.ShowSpinner = false;
            }
            catch (Exception ex)
            {
                AppState.ShowSpinner = false;
                await AlertService.ShowErrorAlertAsync(
                    LocalizationService["Error"],
                    LocalizationService["DeleteAllScanResultsFailed"]
                );
            }
        }

    }
}