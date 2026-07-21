using FluentValidation;
using HRManagement.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace HRManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR bu assembly'yi tarar ve IRequestHandler implementasyonlarının
        // HEPSİNİ kendisi kaydeder. Yeni bir handler yazınca buraya satır eklenmez.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Her mesaj önce ValidationBehavior'dan, sonra handler'dan geçer.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // AbstractValidator'dan türeyen tüm validator'lar otomatik kaydedilir.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
