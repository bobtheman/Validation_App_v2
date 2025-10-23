using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace AccreditValidation
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            EnableImmersiveMode();

            // Make the activity fullscreen and draw behind the status bar
            Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

            // Ensure content fits system windows (for fullscreen)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
            }
            else
            {
#pragma warning disable CS0618 // Suppress obsolete warning for older Android support
                Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
#pragma warning restore CS0618
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                EnableImmersiveMode();
        }

        private void EnableImmersiveMode()
        {
            var uiOptions = (int)Window.DecorView.SystemUiVisibility;
            uiOptions |= (int)(
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.LayoutHideNavigation |
                SystemUiFlags.LayoutStable
            );
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        protected override void OnResume()
        {
            base.OnResume();

        }
    }
}
