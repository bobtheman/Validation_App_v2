namespace AccreditValidation.Components.Services
{
    namespace AccreditValidation.Components.Services
    {
        public static class ServiceHelper
        {
            public static T GetService<T>() where T : class =>
                Current.GetService(typeof(T)) as T ?? throw new Exception($"Service {typeof(T).Name} not found");

            public static IServiceProvider Current =>
#if WINDOWS
                        MauiWinUIApplication.Current.Services;
#elif ANDROID
                        MauiApplication.Current.Services;
#elif IOS
                         MauiUIApplicationDelegate.Current.Services;
#else
                        throw new NotSupportedException("Unsupported platform");
#endif
        }
    }
}
