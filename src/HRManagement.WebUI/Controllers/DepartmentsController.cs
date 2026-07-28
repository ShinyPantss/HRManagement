using HRManagement.WebUI.Models;
using HRManagement.WebUI.Models.Api.Departments;
using HRManagement.WebUI.Models.Departments;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcisi üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
///
/// TÜM ekran yalnızca HR/Admin'e açık (kullanıcı kararı) — menüde gizli olması
/// yetmez, URL ile de girilememeli. Departman VERİSİ (isim) ise API'de açık kalır:
/// Ekibim/İzin gibi ekranlar pozisyon türetmek için onu okur.
/// </summary>
[Authorize(Roles = "HR,Admin")]
public class DepartmentsController : Controller
{
    private readonly IDepartmentApi _departmentApi;
    private readonly IEmployeeApi _employeeApi;
    private readonly IUnitApi _unitApi;
    private readonly IInternApi _internApi;

    public DepartmentsController(
        IDepartmentApi departmentApi,
        IEmployeeApi employeeApi,
        IUnitApi unitApi,
        IInternApi internApi)
    {
        _departmentApi = departmentApi;
        _employeeApi = employeeApi;
        _unitApi = unitApi;
        _internApi = internApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _departmentApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<DepartmentCardViewModel>());
        }

        var departments = response.Data ?? [];

        // Kart sayıları departman ucundan GELMEZ (o uç yalnızca Id/Ad/Açıklama döner);
        // çalışan, birim ve stajyer listelerinden türetilir. Bu ekran zaten HR/Admin'e
        // açık olduğu için üç listenin tamamı görülebilir.
        var employees = (await _employeeApi.GetAllAsync()).Data ?? [];
        var units = (await _unitApi.GetAllAsync()).Data ?? [];
        var interns = (await _internApi.GetAllAsync()).Data ?? [];

        var cards = departments.Select(d =>
        {
            var inDept = employees.Where(e => e.DepartmentId == d.Id).ToList();
            var activeCount = inDept.Count(e => e.IsActive);

            // Yönetici: yönetici kademesindeki (GM/GMY/Müdür) en kıdemli aktif çalışan.
            // Backend'deki UnitManagerResolver ile aynı kural — sayı küçüldükçe kıdem artar.
            var manager = inDept
                .Where(e => e.IsActive && SeniorityDisplay.IsManagerial(e.Seniority))
                .OrderBy(e => e.Seniority)
                .FirstOrDefault();

            return new DepartmentCardViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ActiveCount = activeCount,
                TotalCount = inDept.Count,
                UnitCount = units.Count(u => u.DepartmentId == d.Id),
                InternCount = interns.Count(i => i.DepartmentId == d.Id),
                ManagerName = manager is null ? null : $"{manager.FirstName} {manager.LastName}"
            };
        })
        .OrderByDescending(c => c.ActiveCount)
        .ToList();

        return View(cards);
    }

    public IActionResult Create()
    {
        return View(new DepartmentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var response = await _departmentApi.CreateAsync(new DepartmentRequest
        {
            Name = form.Name,
            Description = form.Description
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Departman oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var response = await _departmentApi.GetByIdAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Departman bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(new DepartmentFormViewModel
        {
            Id = response.Data.Id,
            Name = response.Data.Name,
            Description = response.Data.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DepartmentFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var response = await _departmentApi.UpdateAsync(form.Id, new DepartmentRequest
        {
            Name = form.Name,
            Description = form.Description
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Departman güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _departmentApi.DeleteAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Silme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "Departman silindi.";

        return RedirectToAction(nameof(Index));
    }
}
