using HRManagement.WebUI.Models.Api.Departments;
using HRManagement.WebUI.Models.Departments;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcisi üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
/// </summary>
public class DepartmentsController : Controller
{
    private readonly IDepartmentApi _departmentApi;

    public DepartmentsController(IDepartmentApi departmentApi)
    {
        _departmentApi = departmentApi;
    }

    // Departmanlar ekranı Employee'ye kapalı (kullanıcı kararı) — menüde gizli
    // olması yetmez, URL ile de girilememeli. Departman VERİSİ ise API'de açık
    // kalır: Ekibim/İzin ekranları pozisyon türetmek için onu okur.
    [Authorize(Roles = "HR,Admin,Manager,Intern")]
    public async Task<IActionResult> Index()
    {
        var response = await _departmentApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<DepartmentResponse>());
        }

        return View(response.Data ?? new List<DepartmentResponse>());
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
