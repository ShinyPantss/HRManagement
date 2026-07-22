using System.Security.Claims;
using HRManagement.WebUI.Models.Account;
using HRManagement.WebUI.Models.Api.Auth;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Giriş/çıkış akışı. İş yapmaz: API'ye login isteği atar, dönen JWT'yi
/// cookie ticket'ının içine koyar. Tarayıcı bundan sonra sadece cookie taşır.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthApi _authApi;

    public AccountController(IAuthApi authApi)
    {
        _authApi = authApi;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(form);

        var response = await _authApi.LoginAsync(new LoginApiRequest
        {
            UsernameOrEmail = form.UsernameOrEmail,
            Password = form.Password
        });

        if (!response.IsSuccess || response.Data is null)
        {
            // API tek tip mesaj döner ("Kullanıcı adı veya şifre hatalı.") —
            // hangi alanın yanlış olduğunu sızdırmamak için burada da ayrıştırmıyoruz.
            ModelState.AddModelError(string.Empty, response.Message ?? "Giriş başarısız.");
            return View(form);
        }

        await SignInWithTokenAsync(response.Data, form.RememberMe);

        // LocalRedirect: returnUrl'e dışarıdan "https://kotusite.com" yazılabilir.
        // LocalRedirect yalnızca kendi sitemizdeki yolları kabul eder (open redirect koruması).
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Cookie'yi siler; içindeki JWT de onunla birlikte gider.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInWithTokenAsync(LoginApiResponse data, bool isPersistent)
    {
        // Claim'ler API'nin döndüğü bilgiden kurulur; menü/görünüm kararları bunlara bakar.
        // Bunlar YALNIZCA UI içindir — gerçek yetki kontrolü API'de, JWT üzerinden yapılır.
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, data.Username),
            new(ClaimTypes.Role, data.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties { IsPersistent = isPersistent };

        // JWT'yi ticket'ın içine koy. Şifrelenmiş cookie'de sunucuda kalır;
        // BearerTokenHandler her API çağrısında GetTokenAsync ile buradan okur.
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = data.Token }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
