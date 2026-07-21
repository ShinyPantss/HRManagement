using Microsoft.Extensions.DependencyInjection;

namespace HRManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR bu assembly'yi tarar ve IRequestHandler implementasyonlarının
        // HEPSİNİ kendisi kaydeder. Yeni bir handler yazınca buraya satır eklenmez.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
