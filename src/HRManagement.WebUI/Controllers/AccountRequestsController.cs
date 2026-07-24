using HRManagement.WebUI.Models.AccountRequests;
using HRManagement.WebUI.Models.Api.AccountRequests;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // Manuel "Hesap Talebi Oluştur" ekranı KALDIRILDI: talepler artık çalışan/stajyer
    // eklenirken otomatik açılıyor (form kutusu). Bu controller yalnızca Admin'in
    // bekleyen talepleri işlemesini sağlar.

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
            SuggestedRole = request.SuggestedRole
        };

        // Şirket standardında otomatik doldur (Admin yine düzenleyebilir):
        //   kullanıcı adı / e-posta : HPY + 1{id}  → id 15 ise HPY10015 / hpy10015@hepiyi.com
        //   geçici şifre            : hpy{soyisim}{id} → hpyYılmaz15
        // id, kişinin (çalışan/stajyer) KENDİ id'sidir — talebin değil (kalıcı kimlik).
        var (subjectId, lastName) = await GetSubjectAsync(request);
        if (subjectId > 0)
        {
            form.Username = $"HPY1{subjectId:D4}";
            form.Email = $"hpy1{subjectId:D4}@hepiyi.com";
            form.Password = $"hpy{StripSpaces(lastName)}{subjectId}";
        }

        return View(form);
    }

    // Talebin sahibini (çalışan veya stajyer) çözer: kimlik üretimi için id + soyisim.
    private async Task<(int Id, string LastName)> GetSubjectAsync(AccountRequestResponse request)
    {
        if (request.EmployeeId is int eid)
        {
            var emp = await _employeeApi.GetByIdAsync(eid);
            if (emp.IsSuccess && emp.Data is not null)
                return (emp.Data.Id, emp.Data.LastName);
        }
        else if (request.InternId is int iid)
        {
            var intern = await _internApi.GetByIdAsync(iid);
            if (intern.IsSuccess && intern.Data is not null)
                return (intern.Data.Id, intern.Data.LastName);
        }
        return (0, string.Empty);
    }

    private static string StripSpaces(string value) => value.Replace(" ", string.Empty);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApproveAccountRequestViewModel form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var response = await _accountRequestApi.ApproveAsync(form.Id, new ApproveAccountRequestRequest
        {
            Username = form.Username,
            Email = form.Email,
            Password = form.Password
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
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
}
