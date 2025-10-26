namespace AccreditValidation
{
    using AccreditValidation.Components.Services;
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Shared.Services.AlertService;
    using AccreditValidation.Helper;
    using AccreditValidation.Helper.Interface;
    using CommunityToolkit.Maui;
    using Microsoft.Extensions.Logging;
    using ZXing.Net.Maui.Controls;
    using Plugin.Fingerprint.Abstractions;
    using Plugin.Maui.Audio;
    using Plugin.Fingerprint;

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-solid-900.ttf", "FaSolid");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Core Services
            builder.Services.AddSingleton<IAppState, AppState>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAlertService, AlertService>();
            builder.Services.AddScoped<IAreaService, AreaService>();
            builder.Services.AddScoped<IConnectivityChecker, ConnectivityChecker>();
            builder.Services.AddScoped<IOfflineDataService, OfflineDataService>();
            builder.Services.AddScoped<IRestDataService, RestDataService>();
            builder.Services.AddScoped<IDirectionService, DirectionService>();
            builder.Services.AddScoped<ILocalizationService, LocalizationService>();
            builder.Services.AddSingleton<ILanguageStateService ,LanguageStateService>();
            builder.Services.AddScoped<IVersionProvider, VersionProvider>();
            builder.Services.AddSingleton<IDevicePlaformHelper, DevicePlaformHelper>();
            builder.Services.AddSingleton<IScannerCodeHelper, ScannerCodeHelper>();
            builder.Services.AddSingleton<IFileService, FileService>();

            // Fingerprint Auth
            builder.Services.AddSingleton(typeof(IFingerprint), CrossFingerprint.Current);

            //Audio Service
            builder.Services.AddSingleton(AudioManager.Current);

            // Optional: Uncomment if needed
            // builder.Services.AddSingleton<IFileService, FileService>();
            // builder.Services.AddSingleton<IConnectivityChecker, ConnectivityChecker>();
            // builder.Services.AddSingleton<IOfflineDataService, OfflineDataService>();
            // builder.Services.AddSingleton<IRestDataService, RestDataService>();
            // builder.Services.AddSingleton<ILanguageHelper, LanguageHelper>();

            // Disable safe area insets (iOS only)
            Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping("NoSafeArea", (handler, view) =>
            {
            #if IOS
            handler.PlatformView.InsetsLayoutMarginsFromSafeArea = false;
            #endif
            });

            // Set the current activity resolver
            #if ANDROID
            CrossFingerprint.SetCurrentActivityResolver(() => Platform.CurrentActivity);
#endif

#if ANDROID
            builder.Services.AddSingleton<INotificationService, NotificationService>();
#elif IOS
        builder.Services.AddSingleton<INotificationService, AccreditValidation.Components.Services.NotificationService>();
#elif WINDOWS
        builder.Services.AddSingleton<INotificationService, AccreditValidation.Components.Services.NotificationService>();
#endif

            return builder.Build();
        }
    }
}
