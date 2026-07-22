using HRManagement.Application.Features.Users.Commands.CreateUser;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.API.Seeding;

/// <summary>
/// Tavuk-yumurta problemi: hesapları yalnızca giriş yapmış yetkililer açabiliyor,
/// ama Users tablosu boşken kimse giriş yapamaz. Bu seeder, sistemde HİÇ kullanıcı
/// yokken ilk Admin hesabını açar.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        // Repository'ler ve MediatR "scoped" kayıtlı. Scope'u normalde her HTTP isteği
        // açar; startup'ta henüz istek yok, bu yüzden elle bir scope açıyoruz.
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var userRepository = services.GetRequiredService<IUserRepository>();
        var existingUsers = await userRepository.GetAllAsync();

        // Idempotent: bir kullanıcı varsa kapı zaten açılabilir durumda, dokunma.
        // Her açılışta çalışır ama yalnızca ilk (boş DB) açılışta iş yapar.
        if (existingUsers.Any())
            return;

        // Username/Email appsettings.json'da (gizli değil), Password user-secrets'ta.
        var section = app.Configuration.GetSection("SeedAdmin");
        var username = section["Username"];
        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            // Fail-fast: DB boş + ayar eksik = kimsenin giremeyeceği bir uygulama.
            // Sessizce açılmasındansa nedenini söyleyerek durması daha iyi.
            throw new InvalidOperationException(
                "Users tablosu boş ve SeedAdmin ayarları eksik. Şifreyi user-secrets ile verin: " +
                "dotnet user-secrets set \"SeedAdmin:Password\" \"<sifre>\" --project src/HRManagement.API");
        }

        // Hash'leme, benzersizlik kontrolü ve kayıt zaten CreateUserCommandHandler'da.
        // Seeder o kodu kopyalamaz, var olan use-case'i çağırır: tek doğruluk kaynağı.
        var sender = services.GetRequiredService<ISender>();
        await sender.Send(new CreateUserCommand(username, email, password, Role.Admin));

        app.Logger.LogInformation("Seed admin oluşturuldu: {Username}", username);
    }
}
