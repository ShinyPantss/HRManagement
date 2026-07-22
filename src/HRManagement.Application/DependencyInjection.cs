using FluentValidation;
using HRManagement.Application.Behaviors;
using HRManagement.Application.Features.LeaveRequests.Shared;
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

        // İki aşamalı izin onayının ortak yetki kuralları — Approve ve Reject
        // handler'ları paylaşır ("onaylayabilen reddedebilir" simetrisi tek yerde).
        services.AddScoped<LeaveApprovalGuard>();

        // "Kim hangi çalışanı görebilir" kuralı — liste ve detay sorguları paylaşır.
        services.AddScoped<Features.Employees.Shared.EmployeeVisibility>();

        return services;
    }
}
