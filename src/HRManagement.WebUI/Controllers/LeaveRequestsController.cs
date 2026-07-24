using HRManagement.WebUI.Models.Api.LeaveRequests;
using HRManagement.WebUI.Models.LeaveRequests;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
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
        // Rol kapısı: HR/Admin herkesi tarayabilir (çalışan seçici). Diğer roller
        // yalnızca KENDİ izinlerini görür — seçici gizlenir, liste kendi kaydına sabitlenir.
        var isBrowser = User.IsInRole("HR") || User.IsInRole("Admin");

        var me = await _employeeApi.GetMyProfileAsync();
        var currentEmployeeId = me.IsSuccess ? me.Data?.Id : null;

        var effectiveEmployeeId = isBrowser ? employeeId : currentEmployeeId;

        var model = new LeaveRequestListViewModel
        {
            SelectedEmployeeId = effectiveEmployeeId,
            CurrentEmployeeId = currentEmployeeId,
            ShowEmployeePicker = isBrowser,
            EmployeeOptions = isBrowser ? await GetEmployeeOptionsAsync() : []
        };

        // API'de "tüm talepleri getir" ucu yok: kişi belirlenmeden liste çekilemez.
        if (effectiveEmployeeId is null)
            return View(model);

        var response = await _leaveRequestApi.GetByEmployeeAsync(effectiveEmployeeId.Value);

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "İzin talepleri alınamadı.";
            return View(model);
        }

        model.Requests = response.Data ?? [];
        return View(model);
    }

    // Giriş yapanın ONAYINI BEKLEYEN talepler — tek listede, çalışan seçmeden.
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> Approvals()
    {
        var response = await _leaveRequestApi.GetPendingApprovalsAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "Onay bekleyenler alınamadı.";
            return View(new List<PendingApprovalResponse>());
        }

        return View(response.Data ?? []);
    }

    public IActionResult Create()
    {
        // Çalışan seçimi yok: talep, giriş yapan hesabın kendisi için açılır;
        // kimliği API, JWT claim'inden çözer.
        var form = new LeaveRequestFormViewModel { TypeOptions = GetTypeOptions() };

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel form)
    {
        // Rapor zorunluluğu türe bağlı: yalnızca Hastalık (3) seçiliyse istenir.
        // [Required] türe göre koşullanamadığı için burada elle ekleniyor; nihai
        // otorite yine API + Application validator'ıdır (bu sadece erken UX geri bildirimi).
        const int sickType = 3;
        if (form.Type == sickType && string.IsNullOrWhiteSpace(form.MedicalReport))
            ModelState.AddModelError(nameof(form.MedicalReport), "Hastalık izni için rapor bilgisi zorunludur.");

        if (!ModelState.IsValid)
            return View(FillOptions(form));

        var response = await _leaveRequestApi.CreateAsync(new CreateLeaveRequestRequest
        {
            // ModelState geçerliyse [Required] alanlar dolu; bu yüzden .Value güvenli.
            Type = form.Type,
            StartDate = form.StartDate!.Value,
            EndDate = form.EndDate!.Value,
            Description = form.Description,
            // Rapor yalnızca hastalık izninde anlamlı; diğer türlerde boş gönderilir.
            MedicalReport = form.Type == sickType ? form.MedicalReport : null
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti (hak yetersiz, tarih çakışması vb.) — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(FillOptions(form));
        }

        TempData["Success"] = response.Message ?? "İzin talebi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // returnTo: "Approvals" ise Onay Bekleyenler'e döner, aksi hâlde çalışan listesine.
    [HttpPost]
    [Authorize(Roles = "HR,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, int employeeId, string? returnTo)
    {
        var response = await _leaveRequestApi.ApproveAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Onaylama işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi onaylandı.";

        return returnTo == "Approvals"
            ? RedirectToAction(nameof(Approvals))
            : RedirectToAction(nameof(Index), new { employeeId });
    }

    [HttpPost]
    [Authorize(Roles = "HR,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, int employeeId, string? reason, string? returnTo)
    {
        var response = await _leaveRequestApi.RejectAsync(id, new RejectLeaveRequestRequest
        {
            Reason = reason
        });

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Reddetme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi reddedildi.";

        return returnTo == "Approvals"
            ? RedirectToAction(nameof(Approvals))
            : RedirectToAction(nameof(Index), new { employeeId });
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
    /// Form View'a geri dönerken dropdown TEKRAR doldurulmalı:
    /// POST gövdesinde seçenek listeleri gelmez, sadece seçilen değerler gelir.
    /// </summary>
    private static LeaveRequestFormViewModel FillOptions(LeaveRequestFormViewModel form)
    {
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
