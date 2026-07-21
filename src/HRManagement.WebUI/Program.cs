using HRManagement.WebUI.Services;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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

builder.Services.AddRefitClient<IDepartmentApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddRefitClient<IEmployeeApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddRefitClient<IInternApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddRefitClient<ILeaveRequestApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
