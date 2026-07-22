using HRManagement.API;
using HRManagement.API.Middleware;
using HRManagement.API.Seeding;
using HRManagement.Application;
using HRManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ───── SERVİS KAYDI ─────
// Composition root. Her katman kendi kayıtlarını kendi AddXxx() metodunda toplar;
// burada yalnızca NE'nin kurulduğu görünür, NASIL'ı ilgili katmanda durur.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddApiServices();
builder.Services.AddApiAuthentication(builder.Configuration);

var app = builder.Build();

// ───── MIDDLEWARE BORU HATTI ─────
// Sıra önemlidir: her istek yukarıdan aşağıya bu istasyonlardan geçer.

// En dışta: içeride patlayan her exception → BaseResponse.
app.UseExceptionHandler();

// Hemen ardından: gövdesiz hata yanıtları (401/403/404 ...) → BaseResponse.
app.UseBaseResponseStatusCodes();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication "sen kimsin?" (token'ı çözüp User'ı doldurur),
// Authorization "buna yetkin var mı?" (dolu User üzerinden karar verir).
// Ters yazılırsa yetki kontrolü daima boş kimlikle çalışır.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// İlk açılışta Users tablosu boşsa admin hesabını oluşturur; doluysa hiçbir şey yapmaz.
await app.SeedAdminAsync();

app.Run();
