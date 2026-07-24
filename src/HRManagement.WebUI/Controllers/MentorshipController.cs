using HRManagement.WebUI.Models.Api.Interns;
using HRManagement.WebUI.Models.Api.Mentorship;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Mentorluk ekranları: kişinin KENDİ stajyerleri, stajyer detayı, görev/not
/// işlemleri. Rol attribute'u yok — Manager da Employee da mentor olabilir;
/// "bu stajyerin mentoru mu?" sorusunun cevabı API'dedir (MentorshipGuard).
/// Mentor olmayan kullanıcı yalnızca boş liste görür.
/// </summary>
public class MentorshipController : Controller
{
    private readonly IMentorshipApi _mentorshipApi;

    public MentorshipController(IMentorshipApi mentorshipApi)
    {
        _mentorshipApi = mentorshipApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _mentorshipApi.GetMyInternsAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<InternResponse>());
        }

        return View(response.Data ?? new List<InternResponse>());
    }

    public async Task<IActionResult> Detail(int id)
    {
        var response = await _mentorshipApi.GetDetailAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Stajyer bilgisi alınamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask(int id, string? title, string? description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Görev başlığı boş olamaz.";
        }
        else
        {
            var response = await _mentorshipApi.AddTaskAsync(id, new AddInternTaskRequest
            {
                Title = title.Trim(),
                Description = description,
                DueDate = dueDate
            });

            TempData[response.IsSuccess ? "Success" : "Error"] =
                response.Message ?? (response.IsSuccess ? "Görev atandı." : "Görev atanamadı.");
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Not içeriği boş olamaz.";
        }
        else
        {
            var response = await _mentorshipApi.AddNoteAsync(id, new AddInternNoteRequest
            {
                Content = content.Trim()
            });

            TempData[response.IsSuccess ? "Success" : "Error"] =
                response.Message ?? (response.IsSuccess ? "Not eklendi." : "Not eklenemedi.");
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // id = stajyer (redirect için), taskId = güncellenen görev.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(int id, int taskId, int status)
    {
        var response = await _mentorshipApi.UpdateTaskStatusAsync(taskId, new UpdateInternTaskStatusRequest
        {
            Status = status
        });

        TempData[response.IsSuccess ? "Success" : "Error"] =
            response.Message ?? (response.IsSuccess ? "Görev durumu güncellendi." : "Güncellenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }
}
