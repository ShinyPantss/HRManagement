using HRManagement.WebUI.Models;
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

    public async Task<IActionResult> Index()
    {
        // Rol kapısı: HR/Admin TÜM izin geçmişini tek listede görür (çalışan seçmeden,
        // salt gözlem). Diğer roller yalnızca KENDİ izinlerini görür.
        var isBrowser = User.IsInRole("HR") || User.IsInRole("Admin");

        if (isBrowser)
        {
            var all = await _leaveRequestApi.GetAllAsync();
            var browseModel = new LeaveRequestListViewModel { IsAllView = true };

            if (!all.IsSuccess)
                TempData["Error"] = all.Message ?? "İzin geçmişi alınamadı.";
            else
                browseModel.AllRows = all.Data ?? [];

            return View(browseModel);
        }

        var me = await _employeeApi.GetMyProfileAsync();
        var currentEmployeeId = me.IsSuccess ? me.Data?.Id : null;

        var model = new LeaveRequestListViewModel
        {
            SelectedEmployeeId = currentEmployeeId,
            CurrentEmployeeId = currentEmployeeId
        };

        // Çalışan kaydı yoksa (hesap kişiye bağlı değilse) liste boş kalır.
        if (currentEmployeeId is null)
            return View(model);

        var response = await _leaveRequestApi.GetByEmployeeAsync(currentEmployeeId.Value);

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "İzin talepleri alınamadı.";
            return View(model);
        }

        model.Requests = response.Data ?? [];
        return View(model);
    }

    // Tek talebin detayı + onay izi. Görüntüleme yetkisini API çözer; yetkisizse
    // (403/400) kullanıcı listeye döner. İK aşamasındaysa detaydan onay/red yapılabilir.
    public async Task<IActionResult> Details(int id)
    {
        var response = await _leaveRequestApi.GetDetailAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "İzin detayı alınamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    /// <summary>
    /// Ekip izin takvimi (yalnızca yönetici). Önümüzdeki iki hafta boyunca kimin
    /// izinli olduğunu tek ekranda gösterir.
    ///
    /// Veri iki uçtan birleştirilir: görünür ekip /api/employees'ten, her kişinin
    /// izinleri /api/leaverequests/employee/{id}'den. "Tümünü tek çağrıda ver"
    /// diyen bir uç YOK (/all yalnızca İK/Admin'e açık), bu yüzden kişi başına
    /// bir istek atılır — paralel, ve ekip büyükse hiç çizilmez.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Calendar()
    {
        const int dayCount = 14;
        const int maxTeamSize = 30;   // üstünde sayfa başına 30+ HTTP isteği olurdu

        var today = DateTime.Today;
        var model = new TeamCalendarViewModel
        {
            StartDate = today,
            DayCount = dayCount,
            Days = Enumerable.Range(0, dayCount).Select(i => today.AddDays(i)).ToList()
        };

        var me = await _employeeApi.GetMyProfileAsync();
        var myId = me.Data?.Id;
        var myManagerId = me.Data?.ManagerId;

        var employees = await _employeeApi.GetAllAsync();

        if (!employees.IsSuccess)
        {
            TempData["Error"] = employees.Message ?? "Ekip listesi alınamadı.";
            return View(model);
        }

        // Görünür liste kişinin BİR ÜST yöneticisini de içerir; onun izinlerini
        // çekme yetkimiz yok (API zincir-aşağı bakar), o yüzden listeden düşülür.
        var team = (employees.Data ?? [])
            .Where(e => e.IsActive && e.Id != myManagerId)
            .OrderByDescending(e => e.Id == myId)
            .ThenBy(e => e.Seniority ?? 99)
            .ThenBy(e => e.FirstName)
            .ToList();

        model.TeamSize = team.Count;

        if (team.Count > maxTeamSize)
        {
            model.TeamTooLarge = true;
            return View(model);
        }

        var endDate = today.AddDays(dayCount - 1);

        // Paralel: sıralı atsaydık 20 kişilik ekipte sayfa 20 gidiş-dönüş beklerdi.
        var fetches = team.Select(async e => (Employee: e, Leaves: await _leaveRequestApi.GetByEmployeeAsync(e.Id)));
        var results = await Task.WhenAll(fetches);

        foreach (var (employee, leaves) in results)
        {
            var row = new TeamCalendarRow
            {
                EmployeeId = employee.Id,
                FullName = $"{employee.FirstName} {employee.LastName}",
                Initials = Initials(employee.FirstName, employee.LastName),
                Subtitle = SeniorityDisplay.Label(employee.Seniority),
                IsSelf = employee.Id == myId
            };

            // Reddedilenler takvimde yer tutmaz; onaylı ve bekleyenler tutar
            // (bekleyen izin de planlamada görünmeli).
            var relevant = (leaves.Data ?? [])
                .Where(l => l.Status != "Rejected"
                            && l.StartDate.Date <= endDate
                            && l.EndDate.Date >= today)
                .ToList();

            foreach (var day in model.Days)
            {
                var hit = relevant.FirstOrDefault(l => l.StartDate.Date <= day && l.EndDate.Date >= day);
                var isWeekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                row.Cells.Add(new TeamCalendarCell
                {
                    Date = day,
                    IsWeekend = isWeekend,
                    IsToday = day == today,
                    Type = hit?.Type,
                    Status = hit?.Status
                });

                if (hit is not null && !isWeekend) row.LeaveDays++;
            }

            model.Rows.Add(row);
        }

        // Çakışma: aynı iş gününde 2+ kişi izinli.
        model.ClashDays = model.Days
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .Where(d => model.Rows.Count(r => r.Cells.Any(c => c.Date == d && c.Type is not null)) >= 2)
            .ToList();

        return View(model);
    }

    private static string Initials(string first, string last) =>
        $"{(first.Length > 0 ? char.ToUpperInvariant(first[0]) : '?')}{(last.Length > 0 ? char.ToUpperInvariant(last[0]) : ' ')}".Trim();

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

        return RedirectAfterAction(returnTo, id, employeeId);
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

        return RedirectAfterAction(returnTo, id, employeeId);
    }

    // Onay/Red sonrası nereye dönüleceği: geldiğin ekrana. "Details" ise o talebin
    // detayına (güncel durumu görürsün), "Approvals" ise Onay Bekleyenler'e, yoksa listeye.
    private IActionResult RedirectAfterAction(string? returnTo, int id, int employeeId) =>
        returnTo switch
        {
            "Details" => RedirectToAction(nameof(Details), new { id }),
            "Approvals" => RedirectToAction(nameof(Approvals)),
            _ => RedirectToAction(nameof(Index), new { employeeId })
        };

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

    // İzin türleri Domain enum'ının sayısal karşılıklarıdır; API bu değerleri bekler.
    private static IEnumerable<SelectListItem> GetTypeOptions() =>
    [
        new SelectListItem("Yıllık İzin", "1"),
        new SelectListItem("Ücretsiz İzin", "2"),
        new SelectListItem("Hastalık İzni", "3")
    ];
}
