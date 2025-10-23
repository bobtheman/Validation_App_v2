using CommunityToolkit.Maui.Views;
using Plugin.Maui.Audio;
using ZXing.Net.Maui;

namespace AccreditValidation.Components.Pages.Xaml;

public partial class DeviceCamera : Popup
{
    private readonly IAudioManager _audioManager;

    public DeviceCamera(IAudioManager audioManager)
    {
        InitializeComponent();
        _audioManager = audioManager;
    }

    private async void scanner_Barcode_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        scanner_Barcode.IsDetecting = false;

        try
        {
            var player = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("beep.mp3"));
            player.Play();
        }
        catch (Exception ex)
        { 
        
        }
        
        Close(e.Results[0].Value);
    }
}
