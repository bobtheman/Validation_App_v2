using Foundation;
using UIKit;

namespace AccreditValidation
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // Make status bar transparent or hide it to prevent space
            UIApplication.SharedApplication.StatusBarHidden = true;

            return base.FinishedLaunching(app, options);
        }
    }
}
