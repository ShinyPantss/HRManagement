using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace HRManagement.WebUI.ViewComponents;

/// <summary>
/// Kenar çubuğundaki "Çalışanlar" rozeti: toplam çalışan sayısı.
///
/// Neden ViewComponent? Rozet _Layout'ta yaşar, yani HER sayfada çizilir;
/// controller'ların hiçbiri bu veriyi modeline koymaz. Layout'a veri taşımanın
/// MVC'deki temiz yolu budur — layout'a @inject ile servis sokup çağrı yapmak
/// görünüm dosyasına iş mantığı taşırdı.
///
/// Neden önbellek? Her sayfa yüklemesinde API'ye gitmemek için sayı 5 dakika
/// bellekte tutulur. Rozet bilgilendirme amaçlıdır; dakikası dakikasına doğru
/// olması gerekmez. Sayı alınamazsa rozet HİÇ çizilmez ve hata önbelleğe
/// ALINMAZ — kenar çubuğu bir API arızasında sayfayı düşürmemeli, arıza
/// geçince de ilk istekte kendine gelmeli.
/// </summary>
public class EmployeeCountViewComponent : ViewComponent
{
    private const string CacheKey = "sidebar-employee-count";

    private readonly IEmployeeApi _employeeApi;
    private readonly IMemoryCache _cache;

    public EmployeeCountViewComponent(IEmployeeApi employeeApi, IMemoryCache cache)
    {
        _employeeApi = employeeApi;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Rozet yalnızca İK'da: "Çalışanlar" ekranı İK'da tüm şirketi listeler,
        // diğer rollerde aynı ekran "Ekibim"dir ve kişiye göre daralır — tek bir
        // global sayıyı herkese göstermek yanıltıcı olurdu.
        if (!User.IsInRole("HR"))
            return Content(string.Empty);

        if (!_cache.TryGetValue(CacheKey, out int count))
        {
            var response = await _employeeApi.GetAllAsync();
            if (!response.IsSuccess || response.Data is null)
                return Content(string.Empty);

            count = response.Data.Count;
            _cache.Set(CacheKey, count, TimeSpan.FromMinutes(5));
        }

        return View(count);
    }
}
