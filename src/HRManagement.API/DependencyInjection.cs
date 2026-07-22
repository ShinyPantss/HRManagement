using System.Text;
using HRManagement.API.Middleware;
using HRManagement.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HRManagement.API;

/// <summary>
/// API katmanının servis kayıtları. Application ve Infrastructure kendi AddXxx()
/// metodlarını sunuyor; API de aynı kalıbı izler. Böylece Program.cs "NE kurulduğunu"
/// okunur bir listede tutar, "NASIL kurulduğu" bilgisi buraya iner.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();

        // İşlenmemiş exception'lar → BaseResponse (bkz. GlobalExceptionHandler).
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // UseExceptionHandler() parametresiz çağrıldığında framework BAŞLANGIÇTA
        // "bir son çare tanımlı mı?" diye bakar; yoksa uygulama hiç açılmaz. Bizim
        // handler'ımızın her exception'ı ele aldığını bilemez. AddProblemDetails()
        // eklemek bu kontrolü geçerdi ama projeye ProblemDetails'i sokardı —
        // onun yerine son çareyi de BaseResponse yazacak şekilde tanımlıyoruz.
        // Pratikte buraya düşülmez: GlobalExceptionHandler daima true döner.
        services.Configure<ExceptionHandlerOptions>(options =>
        {
            options.ExceptionHandler = async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(
                    BaseResponse<object>.Fail("Beklenmeyen bir hata oluştu."));
            };
        });

        // [ApiController], model binding hatalarında (bozuk JSON, okunamayan gövde)
        // varsayılan olarak ProblemDetails döndürür ve bu yanıt exception handler'a
        // HİÇ uğramaz — çünkü ortada exception yoktur, MVC kısa devre yapar.
        // Kuralımız "tüm hatalar aynı zarf" olduğu için fabrikayı değiştiriyoruz.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Mesaj bilinçli olarak genel: buraya düşen hatalar JSON ayrıştırma
            // hatalarıdır ("'}' is an invalid start of a value. BytePositionInLine: 9")
            // ve bu metin istemciye yarar sağlamayan bir iç detaydır. Alan bazlı
            // gerçek doğrulama zaten FluentValidation'da yapılıyor.
            options.InvalidModelStateResponseFactory = _ =>
                new BadRequestObjectResult(
                    BaseResponse<object>.Fail("İstek gövdesi okunamadı veya geçersiz."));
        });

        return services;
    }

    /// <summary>
    /// JWT DOĞRULAMA. Token ÜRETİMİ Infrastructure/JwtTokenGenerator'da yapılır;
    /// burası gelen "Authorization: Bearer &lt;token&gt;" başlığını açıp imzayı denetler.
    /// İki taraf da AYNI Jwt:Key'i okur — simetrik imza (HMAC) budur.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
                     ?? throw new InvalidOperationException(
                         "'Jwt:Key' yapılandırması bulunamadı. user-secrets ile verin.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // İmza: token içeriğinin değiştirilmediğini kanıtlar. Kapatılırsa
                    // herkes kendine "Role: Admin" yazan bir token uydurabilir.
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                    // Issuer/Audience: "bu token'ı ben mi ürettim ve bana mı hitap ediyor?"
                    // Aynı anahtarı paylaşan başka bir sistemin token'ını burada geçersiz kılar.
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],

                    // ClockSkew varsayılanı 5 dakikadır (sunucu saatleri kayabilir diye
                    // tolerans). Öğrenirken "2 saat" gerçekten 2 saat olsun.
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // GLOBAL FALLBACK POLICY: üzerinde [Authorize] YAZMAYAN her endpoint de
        // kimlik doğrulaması ister. Yani uçlar "kilitli doğar".
        //
        // Tersi yaklaşım (her uca tek tek [Authorize] yazmak) er ya da geç unutulur
        // ve unutulan uç sessizce herkese açık kalır — üstelik bunu test de yakalamaz,
        // çünkü çalışıyor gibi görünür. Varsayılanı "kapalı" yapıp açılması gereken
        // uçlara bilinçli olarak [AllowAnonymous] koymak güvenli yön.
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
