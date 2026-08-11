using HRManagement.Application.Interfaces;
using HRManagement.Infrastructure.Persistence;
using HRManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // DbConnectionFactory (Dapper) KALMAYA DEVAM EDİYOR. İki yer onu kullanır:
        // asistanın salt okuma sorgu çalıştırıcısı ve çok sonuçlu dashboard SP'si.
        // Geçiş boyunca da her iki dünya yan yana yaşar — repository'ler tek tek çevrilir.
        services.AddSingleton<DbConnectionFactory>();

        // EF Core bağlamı SCOPED: change tracker istek başına yaşamalıdır.
        // Singleton olsaydı bütün isteklerin izlediği entity'ler tek havuzda
        // birikir, hem bellek şişer hem bir isteğin değişikliği diğerine sızardı.
        services.AddDbContext<HRManagementDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "'ConnectionStrings:DefaultConnection' yapılandırması bulunamadı."));

            // UpdatedAt damgası: eskiden 12 repository'deki her UPDATE cümlesinde
            // elle yazılıyordu, artık tek yerde. Interceptor durum tutmaz, o yüzden
            // tek örnek bütün bağlamlara hizmet edebilir.
            options.AddInterceptors(new UpdatedAtInterceptor());
        });

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