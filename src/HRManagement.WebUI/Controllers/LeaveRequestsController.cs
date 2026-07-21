using HRManagement.WebUI.Models.Api.LeaveRequests;
using HRManagement.WebUI.Models.LeaveRequests;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcileri üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
/// İzin talepleri için ayrıca çalışan listesine ihtiyaç var (dropdown + liste filtresi),
/// bu yüzden IEmployeeApi de enjekte edilir.
/// </summary>
public class LeaveRequestsController : Controller
{
    private readonly ILeaveRequestApi _leaveRequestApi;
    private readonly IEmployeeApi _employeeApi;

    public LeaveRequestsController(ILeaveRequestApi leaveRequestApi, IEmployeeApi employeeApi)
    {
        _leaveRequestApi = leaveRequestApi;
        _employeeApi = employeeApi;
    }

    public async Task<IActionResult> Index(int? employeeId)
    {
        var model = new LeaveRequestListViewModel
        {
            SelectedEmployeeId = employeeId,
            EmployeeOptions = await GetEmployeeOptionsAsync()
        };

        // API'de "tüm talepleri getir" ucu yok: çalışan seçilmeden liste çekilemez.
        if (employeeId is null)
            return View(model);

        var response = await _leaveRequestApi.GetByEmployeeAsync(employeeId.Value);

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "İzin talepleri alınamadı.";
            return View(model);
        }

        model.Requests = response.Data ?? [];
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var form = new LeaveRequestFormViewModel
        {
            EmployeeOptions = await GetEmployeeOptionsAsync(),
            TypeOptions = GetTypeOptions()
        };

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(await FillOptionsAsync(form));

        var response = await _leaveRequestApi.CreateAsync(new CreateLeaveRequestRequest
        {
            // ModelState geçerliyse [Required] alanlar dolu; bu yüzden .Value güvenli.
            EmployeeId = form.EmployeeId!.Value,
            Type = form.Type,
            StartDate = form.StartDate!.Value,
            EndDate = form.EndDate!.Value,
            Description = form.Description
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti (hak yetersiz, tarih çakışması vb.) — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(await FillOptionsAsync(form));
        }

        TempData["Success"] = response.Message ?? "İzin talebi oluşturuldu.";
        return RedirectToAction(nameof(Index), new { employeeId = form.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, int employeeId)
    {
        var response = await _leaveRequestApi.ApproveAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Onaylama işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi onaylandı.";

        return RedirectToAction(nameof(Index), new { employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, int employeeId, string? reason)
    {
        var response = await _leaveRequestApi.RejectAsync(id, new RejectLeaveRequestRequest
        {
            Reason = reason
        });

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Reddetme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi reddedildi.";

        return RedirectToAction(nameof(Index), new { employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int employeeId)
    {
        var response = await _leaveRequestApi.DeleteAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Silme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi silindi.";

        return RedirectToAction(nameof(Index), new { employeeId });
    }

    /// <summary>
    /// Form View'a geri dönerken dropdown'lar TEKRAR doldurulmalı:
    /// POST gövdesinde seçenek listeleri gelmez, sadece seçilen değerler gelir.
    /// </summary>
    private async Task<LeaveRequestFormViewModel> FillOptionsAsync(LeaveRequestFormViewModel form)
    {
        form.EmployeeOptions = await GetEmployeeOptionsAsync();
        form.TypeOptions = GetTypeOptions();
        return form;
    }

    private async Task<IEnumerable<SelectListItem>> GetEmployeeOptionsAsync()
    {
        var response = await _employeeApi.GetAllAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Çalışan listesi alınamadı.";
            return [];
        }

        return response.Data
            .Select(employee => new SelectListItem($"{employee.FirstName} {employee.LastName}", employee.Id.ToString()))
            .ToList();
    }

    // İzin türleri Domain enum'ının sayısal karşılıklarıdır; API bu değerleri bekler.
    private static IEnumerable<SelectListItem> GetTypeOptions() =>
    [
        new SelectListItem("Yıllık İzin", "1"),
        new SelectListItem("Ücretsiz İzin", "2"),
        new SelectListItem("Hastalık İzni", "3")
    ];
}
