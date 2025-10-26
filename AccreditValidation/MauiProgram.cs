namespace AccreditValidation
{
    using AccreditValidation.Components.Services;
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Helper;
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Shared.Services.AlertService;
    using CommunityToolkit.Maui;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Plugin.Fingerprint;
    using Plugin.Fingerprint.Abstractions;
    using Plugin.Maui.Audio;
    using System.Reflection;
    using ZXing.Net.Maui.Controls;

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();
            var builder = MauiApp.CreateBuilder();
            
            // Add configuration
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("AccreditValidation.appsettings.json");
            
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
                
            builder.Configuration.AddConfiguration(config);

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
            // Enhanced logging for SignalR debugging
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
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
            builder.Services.AddSingleton<ILanguageStateService, LanguageStateService>();
            builder.Services.AddScoped<IVersionProvider, VersionProvider>();
            builder.Services.AddSingleton<IDevicePlaformHelper, DevicePlaformHelper>();
            builder.Services.AddSingleton<IScannerCodeHelper, ScannerCodeHelper>();
            builder.Services.AddSingleton<IFileService, FileService>();

            // Notification Service - UPDATED for SignalR support
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // Fingerprint Auth
            builder.Services.AddSingleton(typeof(IFingerprint), CrossFingerprint.Current);

            // Audio Service
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

            return builder.Build();
        }
    }
}