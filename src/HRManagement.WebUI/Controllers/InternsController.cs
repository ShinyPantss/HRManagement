using HRManagement.WebUI.Models.Api.Interns;
using HRManagement.WebUI.Models.Interns;
using HRManagement.WebUI.Models.Units;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcileri üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
/// Departman listesi ayrı bir uçtan geldiği için IDepartmentApi de enjekte edilir.
/// </summary>
public class InternsController : Controller
{
    private readonly IInternApi _internApi;
    private readonly IDepartmentApi _departmentApi;
    private readonly IUnitApi _unitApi;

    public InternsController(IInternApi internApi, IDepartmentApi departmentApi, IUnitApi unitApi)
    {
        _internApi = internApi;
        _departmentApi = departmentApi;
        _unitApi = unitApi;
    }

    // Stajyer LİSTESİ yönetim ekranıdır: Manager ve Employee göremez (onlar
    // kendi stajyerlerine Mentorluk'tan ulaşır). Menüde gizli olması yetmez,
    // URL ile de girilememeli.
    [Authorize(Roles = "HR,Admin,Intern")]
    public async Task<IActionResult> Index()
    {
        var response = await _internApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<InternResponse>());
        }

        return View(response.Data ?? new List<InternResponse>());
    }

    // Stajyer kaydı AÇMAK yalnızca İK işidir (çalışan tarafıyla aynı kural).
    // Bu kontroller UX içindir; son söz her zaman API'dedir.
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create()
    {
        var form = new InternFormViewModel();
        await FillDepartmentOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InternFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // Form geri gösterilecek — dropdown seçenekleri POST'ta gelmediği için yeniden doldur.
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        var response = await _internApi.CreateAsync(ToRequest(form));

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Stajyer oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await _internApi.GetByIdAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Stajyer bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var form = new InternFormViewModel
        {
            Id = response.Data.Id,
            FirstName = response.Data.FirstName,
            LastName = response.Data.LastName,
            Email = response.Data.Email,
            University = response.Data.University,
            Major = response.Data.Major,
            Grade = response.Data.Grade,
            StartDate = response.Data.StartDate,
            EndDate = response.Data.EndDate,
            MentorId = response.Data.MentorId,
            DepartmentId = response.Data.DepartmentId,
            UnitId = response.Data.UnitId
        };

        await FillDepartmentOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "HR,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InternFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        var response = await _internApi.UpdateAsync(form.Id, ToRequest(form));

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Stajyer güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _internApi.DeleteAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Silme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "Stajyer silindi.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Form modelini API isteğine çevirir. Buraya yalnızca ModelState geçerliyken
    /// gelinir; bu yüzden nullable alanların değeri kesindir.
    /// </summary>
    private static InternRequest ToRequest(InternFormViewModel form) => new()
    {
        FirstName = form.FirstName,
        LastName = form.LastName,
        Email = form.Email,
        University = form.University,
        Major = form.Major,
        Grade = form.Grade,
        StartDate = form.StartDate!.Value,
        EndDate = form.EndDate!.Value,
        MentorId = form.MentorId,
        DepartmentId = form.DepartmentId!.Value,
        UnitId = form.UnitId,
        // Yalnızca oluşturmada anlamlı; güncellemede API bu alanı yok sayar.
        RequestLoginAccount = form.RequestLoginAccount
    };

    /// <summary>
    /// Departman dropdown'ını API'den doldurur. Liste çekilemezse form yine de
    /// gösterilir (seçenekler boş kalır) — kullanıcı hata mesajını görür.
    /// </summary>
    private async Task FillDepartmentOptionsAsync(InternFormViewModel form)
    {
        var response = await _departmentApi.GetAllAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Departman listesi alınamadı.";
            form.DepartmentOptions = [];
            return;
        }

        form.DepartmentOptions = response.Data
            .Select(department => new SelectListItem
            {
                Text = department.Name,
                Value = department.Id.ToString()
            })
            .ToList();

        // Tüm birimler; view'daki dropdown seçilen departmana göre JS ile süzülür.
        var units = await _unitApi.GetAllAsync();
        form.UnitCandidates = (units.Data ?? [])
            .Select(u => new UnitOption(u.Id, u.Name, u.DepartmentId))
            .ToList();
    }
}
