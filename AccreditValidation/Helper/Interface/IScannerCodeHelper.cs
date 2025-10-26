namespace AccreditValidation.Helper.Interface
{
    using Honeywell.AIDC.CrossPlatform;

    public interface IScannerCodeHelper
    {
        Task<string> GetReaderList();
        void OpenBarcodeReader(BarcodeReader barcodeReader);
        void CloseBarcodeScanner(BarcodeReader barcodeReader, bool bSoftOneShotScanStarted);
    }
}
