namespace AccreditValidation.Helper
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Shared.Constants;
    using Honeywell.AIDC.CrossPlatform;
    using Microsoft.AspNetCore.Components;

    public class ScannerCodeHelper : IScannerCodeHelper
    {
        public static Dictionary<string, object> settings = new Dictionary<string, object>();
        public static string SelectedScannerName { get; set; }

        [Inject]
        private ILocalizationService LocalizationService { get; set; }

        public async Task<string> GetReaderList()
        {
            var scanList = await GetReaderNames();

            if (scanList.Count > 0)
            {
                return scanList[0].ToString();
            }

            return ConstantsName.DefaultReaderKey;
        }

        public async void OpenBarcodeReader(BarcodeReader barcodeReader)
        {
            if (!barcodeReader.IsReaderOpened)
            {
                BarcodeReaderBase.Result result = await barcodeReader.OpenAsync();

                if (result.Code == BarcodeReaderBase.Result.Codes.SUCCESS ||
                    result.Code == BarcodeReaderBase.Result.Codes.READER_ALREADY_OPENED)
                {
                    SetScannerAndSymbologySettings(barcodeReader);
                }
                else
                {
                    DisplayAlert(LocalizationService["AnErrorOccured"].ToString(), LocalizationService["PleaseTryAgain"].ToString(), LocalizationService["OK"].ToString());
                }
            }
        }

        public async void CloseBarcodeScanner(BarcodeReader barcodeReader, bool bSoftOneShotScanStarted)
        {
            if (barcodeReader != null && barcodeReader.IsReaderOpened)
            {
                if (bSoftOneShotScanStarted)
                {
                    await barcodeReader.SoftwareTriggerAsync(false);
                    bSoftOneShotScanStarted = false;
                }

                BarcodeReaderBase.Result result = await barcodeReader.CloseAsync();
                if (result.Code != BarcodeReaderBase.Result.Codes.SUCCESS)
                {
                    DisplayAlert(LocalizationService["AnErrorOccured"].ToString(), LocalizationService["PleaseTryAgain"].ToString(), LocalizationService["OK"].ToString());
                }
            }

            barcodeReader = null;
        }

        private async Task<List<string>> GetReaderNames()
        {
            IList<BarcodeReaderInfo> readerList = null;
            List<string> readerNames = new List<string>();
            try
            {
                readerList = await BarcodeReader.GetConnectedBarcodeReaders();
                if (readerList.Count > 0)
                {
                    foreach (BarcodeReaderInfo reader in readerList)
                    {
                        readerNames.Add(reader.ScannerName);
                    }
                }
                else
                {
                    readerNames.Add(ConstantsName.DefaultReaderKey);
                }

                return readerNames;
            }
            catch (Exception ex)
            {
                readerNames.Add(ConstantsName.DefaultReaderKey);
                DisplayAlert(LocalizationService["AnErrorOccured"].ToString(), LocalizationService["PleaseTryAgain"].ToString(), LocalizationService["OK"].ToString());
                return readerNames;
            }
        }


        private async void SetScannerAndSymbologySettings(BarcodeReader barcodeReader)
        {
            try
            {
                if (barcodeReader.IsReaderOpened)
                {
                    Dictionary<string, object> settings = new Dictionary<string, object>()
                    {
                        {barcodeReader.SettingKeys.TriggerScanMode, barcodeReader.SettingValues.TriggerScanMode_OneShot },
                        {barcodeReader.SettingKeys.Code128Enabled, true },
                        {barcodeReader.SettingKeys.Code39Enabled, true },
                        {barcodeReader.SettingKeys.Ean8Enabled, true },
                        {barcodeReader.SettingKeys.Ean8CheckDigitTransmitEnabled, true },
                        {barcodeReader.SettingKeys.Ean13Enabled, true },
                        {barcodeReader.SettingKeys.Ean13CheckDigitTransmitEnabled, true },
                        {barcodeReader.SettingKeys.Interleaved25Enabled, true },
                        {barcodeReader.SettingKeys.Interleaved25MaximumLength, 100 },
                        {barcodeReader.SettingKeys.Postal2DMode, barcodeReader.SettingValues.Postal2DMode_Usps }
                    };

                    BarcodeReaderBase.Result result = await barcodeReader.SetAsync(settings);
                    if (result.Code != BarcodeReaderBase.Result.Codes.SUCCESS)
                    {
                        DisplayAlert(LocalizationService["AnErrorOccured"].ToString(), LocalizationService["PleaseTryAgain"].ToString(), LocalizationService["OK"].ToString());
                    }
                }
            }
            catch (Exception exp)
            {
                DisplayAlert(LocalizationService["AnErrorOccured"].ToString(), LocalizationService["PleaseTryAgain"].ToString(), LocalizationService["OK"].ToString());
            }
        }

        public static void DisplayAlert(string title, string message, string confirmMessage)
        {
            Application.Current.MainPage.DisplayAlert(title, message, confirmMessage);
        }
    }
}
