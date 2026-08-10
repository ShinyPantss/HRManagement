using System.Security.Cryptography;
using System.Text;
using HRManagement.API.Models;

namespace HRManagement.API.Middleware;

/// <summary>
/// İSTEMCİ kimliği: "bu istek benim WebUI'ımdan mı geliyor?" sorusunu cevaplar.
///
/// Dikkat — bu KULLANICI kimliği DEĞİLDİR. Kullanıcının gerçekten var olup olmadığı
/// yine login'de DB'ye gidilerek doğrulanır. Buradaki anahtarın işlevi, tanımadığımız
/// bir istemcinin isteğini JWT çözülmeden ve DB'ye hiç uğranmadan kapıda elemektir.
/// Özellikle [AllowAnonymous] olan /api/auth/login için değerli: o uç, dışarıya açık
/// tek kapımız.
///
/// Anahtar tarayıcıya asla çıkmaz: WebUI → API çağrıları sunucudan sunucuya yapılır
/// (bu yüzden CORS de yok), başlık kullanıcının tarayıcısında hiç görünmez.
/// </summary>
public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    // MapOpenApi bir endpoint'tir ve endpoint'ler boru hattının SONUNDA çalışır;
    // yani bu middleware /openapi/v1.json isteğini de yakalar. Belgeye tarayıcıdan
    // (başlıksız) bakılabilsin diye muaf tutuyoruz. Zaten yalnızca Development'ta kayıtlı.
    private static readonly string[] ExemptPathPrefixes = ["/openapi"];

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedKeyHash;

    public ApiKeyMiddleware(RequestDelegate next, string expectedKey)
    {
        _next = next;

        // Anahtarı düz metin tutmak yerine hash'ini saklıyoruz; karşılaştırma da
        // hash üzerinden yapılıyor. Nedeni aşağıda, IsKeyValid'de.
        _expectedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request.Path) || IsKeyValid(context.Request))
        {
            await _next(context);
            return;
        }

        // Gövdeyi kendimiz yazıyoruz: boş bıraksak UseBaseResponseStatusCodes zarfı
        // giydirirdi ama mesajı "Giriş yapmanız gerekiyor." olurdu — oysa sorun
        // kullanıcının girişi değil, istemcinin tanınmaması. Mesaj bilinçli olarak
        // kısa: dışarıya mekanizmanın detayını anlatmıyoruz.
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            BaseResponse<object>.Fail("İstemci doğrulanamadı."));
    }

    private static bool IsExempt(PathString path) =>
        ExemptPathPrefixes.Any(prefix =>
            path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    private bool IsKeyValid(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var values))
            return false;

        var providedKey = values.ToString();

        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        // Neden düz "==" değil: string karşılaştırması ilk farklı karakterde durur,
        // yani doğru tahmin edilen her karakter yanıtı ölçülebilir biçimde geciktirir
        // (timing attack — anahtar karakter karakter tahmin edilebilir).
        // FixedTimeEquals her zaman aynı süreyi harcar.
        //
        // Neden hash'lerini karşılaştırıyoruz: FixedTimeEquals uzunluklar farklıysa
        // hemen false döner ve bu, anahtarın UZUNLUĞUNU sızdırır. SHA-256 çıktıları
        // daima 32 bayt olduğu için o sızıntı da kapanır.
        var providedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));

        return CryptographicOperations.FixedTimeEquals(_expectedKeyHash, providedKeyHash);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    /// <summary>
    /// Boru hattındaki yeri önemlidir (bkz. Program.cs): UseBaseResponseStatusCodes'un
    /// ALTINDA — hata yanıtı gerekirse zarf mekanizması hâlâ devrede olsun; ve
    /// UseAuthentication'ın ÜSTÜNDE — amaç, tanınmayan istemciyi token çözülmeden elemek.
    /// </summary>
    public static IApplicationBuilder UseApiKeyValidation(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        // Jwt:Key ile aynı ilke: sır yapılandırmada yoksa uygulama hiç açılmasın.
        // Sessizce "anahtar kontrolü kapalı" moduna düşmek, korumayı fark ettirmeden
        // yok eder — en tehlikeli hata biçimi budur.
        var apiKey = configuration["ApiKey"]
                     ?? throw new InvalidOperationException(
                         "'ApiKey' yapılandırması bulunamadı. user-secrets ile verin.");

        return app.UseMiddleware<ApiKeyMiddleware>(apiKey);
    }
}
