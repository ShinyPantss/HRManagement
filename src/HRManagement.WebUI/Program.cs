using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// API'deki global fallback policy ile aynı ilke: her sayfa giriş ister,
// açık kalması gerekenler ([AllowAnonymous]) bilinçli istisnadır.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

// Tarayıcı ↔ WebUI kimliği: COOKIE. (WebUI ↔ API kimliği ise JWT — BearerTokenHandler'a bak.)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";           // yetkisiz istekte buraya yönlendirilir
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // girişli ama rolü yetersizse
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // API token'ıyla aynı ömür
        options.SlidingExpiration = false;              // token yenilenmediği için cookie de uzamamalı

        // Token cookie'nin içinde taşındığı için JS'in okuyamaması kritik (XSS koruması).
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// BearerTokenHandler cookie ticket'ına HttpContext üzerinden ulaşır.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

// WebUI'nin API'ye tek çıkış kapısı: Refit istemcileri.
// Base adres config'ten gelir; WebUI hiçbir iş katmanına referans vermez.
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("'ApiSettings:BaseUrl' yapılandırması bulunamadı.");

// API hata durumlarında da BaseResponse döndüğü için Refit'in exception fırlatmasını
// kapatıyoruz; yanıt gövdesi her hâlükârda BaseResponse olarak okunur (IsSuccess=false).
var refitSettings = new RefitSettings
{
    ExceptionFactory = _ => Task.FromResult<Exception?>(null)
};

// Login istemcisi: token handler'ı YOK — giriş anında ortada token olmaz.
builder.Services.AddRefitClient<IAuthApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));

// Veri istemcileri: her isteğe JWT'yi ekleyen handler zincire takılır.
builder.Services.AddRefitClient<IDepartmentApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddRefitClient<IEmployeeApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddRefitClient<IInternApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddRefitClient<ILeaveRequestApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddRefitClient<IAccountRequestApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// API'deki ile aynı sıra: önce "sen kimsin" (cookie çözülür), sonra "yetkin var mı".
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
