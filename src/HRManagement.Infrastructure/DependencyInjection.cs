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
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}