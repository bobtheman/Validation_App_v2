namespace AccreditValidation;

using AccreditValidation.Components.Services;
using AccreditValidation.Components.Services.Interface;
using AccreditValidation.Platforms.Android;
using Microsoft.Extensions.DependencyInjection;

public static class NfcServiceRegistration
{
    public static IServiceCollection AddNfcService(this IServiceCollection services)
    {
        services.AddSingleton<INfcService, NfcAndroidService>();
        return services;
    }
}
