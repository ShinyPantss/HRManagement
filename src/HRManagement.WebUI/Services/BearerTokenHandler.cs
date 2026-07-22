using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Her API isteğine, cookie ticket'ında saklanan JWT'yi "Authorization: Bearer ..."
/// başlığı olarak ekler.
///
/// Neden böyle: tarayıcı ↔ WebUI arası kimlik COOKIE ile, WebUI ↔ API arası kimlik
/// JWT ile taşınır. Token tarayıcıya hiç verilmez (JS okuyamaz, localStorage'a yazılmaz);
/// yalnızca sunucudaki cookie ticket'ının içinde durur ve buradan okunup eklenir.
/// Bu sayede her Refit çağrısında elle token geçirmek gerekmez.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            // Giriş yapılmamışsa null döner; o zaman başlık eklenmez ve API 401 verir.
            var token = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
