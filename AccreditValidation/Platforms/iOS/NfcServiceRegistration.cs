namespace AccreditValidation;

using AccreditValidation.Components.Services;
using AccreditValidation.Components.Services.Interface;
using AccreditValidation.Platforms.iOS;
using Microsoft.Extensions.DependencyInjection;

public static class NfcServiceRegistration
{
    public static IServiceCollection AddNfcService(this IServiceCollection services)
    {
        services.AddSingleton<INfcService, NfcIosService>();
        return services;
    }
}
