using HRManagement.WebUI.Models.AccountRequests;
using HRManagement.WebUI.Models.Api.AccountRequests;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Hesap talebi akışının UI tarafı. İş yapmaz; Refit ile API'yi çağırır.
/// Yetki hem burada (rol attribute'ları) hem API'de zorlanır — WebUI kontrolü
/// UX içindir, otorite API'dir.
///
///   Talep oluştur  → yalnızca HR (görev ayrımı: veri girişi HR'da)
///   Bekleyen/Onayla/Reddet → yalnızca Admin (yetkilendirme Admin'de)
///
/// Sınıf düzeyinde yalnızca "giriş şart" konur; rol her action'da ayrı ayrı
/// belirtilir. Sınıf+metot [Authorize]'ları AND'lendiği için sınıfa rol koymak
/// Admin action'larını "HR ve Admin" gibi imkânsız bir şarta sokardı.
/// </summary>
[Authorize]
public class AccountRequestsController : Controller
{
    private readonly IAccountRequestApi _accountRequestApi;
    private readonly IEmployeeApi _employeeApi;
    private readonly IInternApi _internApi;

    public AccountRequestsController(
        IAccountRequestApi accountRequestApi,
        IEmployeeApi employeeApi,
        IInternApi internApi)
    {
        _accountRequestApi = accountRequestApi;
        _employeeApi = employeeApi;
        _internApi = internApi;
    }

    // ── HR: talep oluştur ────────────────────────────────────────────────
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create()
    {
        var form = new CreateAccountRequestViewModel();
        await FillOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountRequestViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        var (employeeId, internId) = ParseSubject(form.Subject);

        var response = await _accountRequestApi.CreateAsync(new CreateAccountRequestRequest
        {
            EmployeeId = employeeId,
            InternId = internId,
            SuggestedRole = form.SuggestedRole,
            Note = form.Note
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Hesap talebi oluşturuldu.";
        return RedirectToAction("Index", "Home");
    }

    // ── Admin: bekleyen talepler ─────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending()
    {
        var response = await _accountRequestApi.GetPendingAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<AccountRequestResponse>());
        }

        return View(response.Data ?? new List<AccountRequestResponse>());
    }

    // ── Admin: onayla ────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var pending = await _accountRequestApi.GetPendingAsync();
        var request = pending.Data?.FirstOrDefault(r => r.Id == id);

        if (request is null)
        {
            TempData["Error"] = "Talep bulunamadı veya artık bekleyen durumda değil.";
            return RedirectToAction(nameof(Pending));
        }

        var form = new ApproveAccountRequestViewModel
        {
            Id = request.Id,
            SubjectName = request.SubjectName,
            SubjectType = request.SubjectType,
            SuggestedRole = request.SuggestedRole,
            RoleOptions = RoleOptions()
        };

        // Çalışan talebinde e-posta ve kullanıcı adını öneri olarak dolduralım.
        if (request.EmployeeId is int eid)
        {
            var emp = await _employeeApi.GetByIdAsync(eid);
            if (emp.IsSuccess && emp.Data is not null)
            {
                form.Email = emp.Data.Email;
                form.Username = emp.Data.Email.Split('@')[0];
            }
        }

        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApproveAccountRequestViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.RoleOptions = RoleOptions();
            return View(form);
        }

        var response = await _accountRequestApi.ApproveAsync(form.Id, new ApproveAccountRequestRequest
        {
            Username = form.Username,
            Email = form.Email,
            Password = form.Password,
            Role = form.Role
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            form.RoleOptions = RoleOptions();
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Talep onaylandı, hesap açıldı.";
        return RedirectToAction(nameof(Pending));
    }

    // ── Admin: reddet ────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var response = await _accountRequestApi.RejectAsync(id, new RejectAccountRequestRequest
        {
            Reason = reason
        });

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Reddetme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "Talep reddedildi.";

        return RedirectToAction(nameof(Pending));
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────

    // Kişi seçenekleri: hesabı OLMAYAN çalışanlar ve stajyerler tek listede.
    // Değer "e:{id}"/"i:{id}" olarak kodlanır ki tek dropdown iki tabloyu ayırsın.
    private async Task FillOptionsAsync(CreateAccountRequestViewModel form)
    {
        var options = new List<SelectListItem>();

        var employees = await _employeeApi.GetAllAsync();
        foreach (var e in (employees.Data ?? []).Where(e => e.UserId is null))
            options.Add(new SelectListItem($"[Çalışan] {e.FirstName} {e.LastName}", $"e:{e.Id}"));

        var interns = await _internApi.GetAllAsync();
        foreach (var i in (interns.Data ?? []).Where(i => i.UserId is null))
            options.Add(new SelectListItem($"[Stajyer] {i.FirstName} {i.LastName}", $"i:{i.Id}"));

        form.SubjectOptions = options;
        form.RoleOptions = RoleOptions();
    }

    private static (int? EmployeeId, int? InternId) ParseSubject(string? subject)
    {
        if (!string.IsNullOrEmpty(subject) && subject.Length > 2 && int.TryParse(subject[2..], out var id))
        {
            if (subject.StartsWith("e:")) return (id, null);
            if (subject.StartsWith("i:")) return (null, id);
        }
        return (null, null); // validator zaten Subject'i zorunlu tutuyor
    }

    // Rol önerileri: Admin bilinçli olarak listede yok — hesap talebiyle Admin
    // yükseltmesi yapılmasın (yine de API'de Admin son sözü söyler).
    private static IEnumerable<SelectListItem> RoleOptions() =>
    [
        new SelectListItem("Çalışan", "4"),
        new SelectListItem("Yönetici", "3"),
        new SelectListItem("İK (HR)", "2"),
        new SelectListItem("Stajyer", "5")
    ];
}
