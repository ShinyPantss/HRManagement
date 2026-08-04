using HRManagement.Application.Interfaces;
using HRManagement.Infrastructure.Persistence;
using HRManagement.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HRManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<DbConnectionFactory>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IInternRepository, InternRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IEmployeeNoteRepository, EmployeeNoteRepository>();
        services.AddScoped<IInternTaskRepository, InternTaskRepository>();
        services.AddScoped<IInternNoteRepository, InternNoteRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRequestRepository, AccountRequestRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Asistan: salt okuma sorgu çalıştırıcı + Claude istemcisi + sohbet geçmişi.
        // ClaudeAssistant SINGLETON — içinde HttpClient taşıyan bir istemci
        // barındırır; scoped olsaydı her istekte yeni soket açardı.
        // MemoryConversationStore da SINGLETON: geçmiş istekler ARASINDA yaşamalı,
        // scoped olsaydı her istekte boş bir depo doğar ve hafıza hiç çalışmazdı.
        services.AddMemoryCache();
        services.AddScoped<ISqlQueryRunner, ReadOnlySqlQueryRunner>();
        services.AddSingleton<IAiAssistant, Ai.ClaudeAssistant>();
        services.AddSingleton<IConversationStore, Ai.MemoryConversationStore>();

        return services;
    }
}