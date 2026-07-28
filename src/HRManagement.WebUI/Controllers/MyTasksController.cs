using HRManagement.WebUI.Models.Api.Mentorship;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Stajyerin "Görevlerim" ekranı: mentorunun atadığı görevleri görür ve
/// durumlarını ilerletir (Başlat/Tamamla). Görev sahipliği API'de denetlenir —
/// stajyer başkasının görevine taskId yazarak da dokunamaz.
/// </summary>
[Authorize(Roles = "Intern")]
public class MyTasksController : Controller
{
    private readonly IInternApi _internApi;

    public MyTasksController(IInternApi internApi)
    {
        _internApi = internApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _internApi.GetMyTasksAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Görevler alınamadı.";
            return RedirectToAction("Index", "Home");
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int taskId, int status)
    {
        var response = await _internApi.UpdateMyTaskStatusAsync(taskId, new UpdateInternTaskStatusRequest
        {
            Status = status
        });

        TempData[response.IsSuccess ? "Success" : "Error"] =
            response.Message ?? (response.IsSuccess ? "Görev durumu güncellendi." : "Güncellenemedi.");

        return RedirectToAction(nameof(Index));
    }
}
