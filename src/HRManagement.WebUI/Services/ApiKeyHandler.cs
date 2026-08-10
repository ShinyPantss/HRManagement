namespace HRManagement.WebUI.Services;

/// <summary>
/// Her API isteğine, WebUI ile API arasında paylaşılan gizli anahtarı
/// "X-Api-Key" başlığı olarak ekler.
///
/// Neden böyle: API'nin "bu istek benim WebUI'ımdan mı geliyor?" sorusuna cevap
/// verebilmesi için. BearerTokenHandler KULLANICIYI tanıtır (JWT), bu handler ise
/// İSTEMCİYİ (uygulamayı) tanıtır. İkisi farklı sorulardır, bu yüzden ayrı handler'lar.
///
/// Neden Refit metotlarına [Header] yazmıyoruz: on iki arayüzdeki her metoda tek tek
/// eklemek gerekirdi ve yeni eklenen bir metot sessizce başlıksız kalırdı. Handler
/// istemcinin tamamına takılır — unutma ihtimali yok.
///
/// Anahtar tarayıcıya hiç verilmez: bu istek sunucudan çıkar, kullanıcının tarayıcısı
/// bu başlığı görmez. JWT'de olduğu gibi JS'e/localStorage'a asla sızdırılmaz.
/// </summary>
public sealed class ApiKeyHandler : DelegatingHandler
{
    private const string HeaderName = "X-Api-Key";

    private readonly string _apiKey;

    public ApiKeyHandler(IConfiguration configuration)
    {
        _apiKey = configuration["ApiSettings:ApiKey"]
                  ?? throw new InvalidOperationException(
                      "'ApiSettings:ApiKey' yapılandırması bulunamadı. user-secrets ile verin.");
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Önce Remove: aynı istek nesnesi yeniden gönderilirse başlık iki kez eklenir
        // ve API tarafında "anahtar,anahtar" olarak birleşip geçersiz olurdu.
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, _apiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
