using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace AccreditValidation
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            this.Padding = new Thickness(0);

            // Disable safe area to avoid automatic padding on iOS
            this.On<iOS>().SetUseSafeArea(false);

            // Correctly configure the Android-specific settings using the appropriate method
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(this, WindowSoftInputModeAdjust.Resize);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

#if ANDROID
            var platformView = blazorWebView?.Handler?.PlatformView as Android.Webkit.WebView;
            if (platformView != null)
            {
                platformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            }
#endif

#if IOS
                this.Padding = new Thickness(0);
                var platformView = blazorWebView?.Handler?.PlatformView as UIKit.UIView;
                if (platformView != null)
                {
                    platformView.BackgroundColor = UIKit.UIColor.Clear;
                }
#endif

#if WINDOWS
                var platformView = blazorWebView?.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.WebView2;
                if (platformView != null)
                {
                    platformView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
                }
#endif
        }
    }
}
