using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Stajyerin "Profilim" ekranı — çalışan tarafındaki Employees/Profile'ın
/// stajyer karşılığı. Veri /api/interns/me'den gelir; kimlik token'dan çözülür,
/// stajyer yalnızca kendi profilini görebilir.
/// </summary>
[Authorize(Roles = "Intern")]
public class InternProfileController : Controller
{
    private readonly IInternApi _internApi;

    public InternProfileController(IInternApi internApi)
    {
        _internApi = internApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _internApi.GetMyProfileAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Profil bilgisi alınamadı.";
            return RedirectToAction("Index", "Home");
        }

        return View(response.Data);
    }
}
