using HRManagement.WebUI.Models.Api.Employees;
using HRManagement.WebUI.Models.Employees;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcileri üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
/// Departman istemcisi yalnızca formdaki dropdown'ı doldurmak için enjekte edilir.
/// </summary>
public class EmployeesController : Controller
{
    private readonly IEmployeeApi _employeeApi;
    private readonly IDepartmentApi _departmentApi;

    public EmployeesController(IEmployeeApi employeeApi, IDepartmentApi departmentApi)
    {
        _employeeApi = employeeApi;
        _departmentApi = departmentApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _employeeApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<EmployeeResponse>());
        }

        return View(response.Data ?? new List<EmployeeResponse>());
    }

    public async Task<IActionResult> Create()
    {
        var form = new EmployeeFormViewModel { IsActive = true };
        await FillDepartmentOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // Form geri gösterilecekse dropdown'ı yeniden doldurmalıyız;
            // seçenekler post gövdesiyle geri gelmez.
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        var response = await _employeeApi.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            NationalId = form.NationalId,
            Email = form.Email,
            Phone = form.Phone,
            // ModelState geçerliyse [Required] sayesinde bu alanlar dolu.
            BirthDate = form.BirthDate!.Value,
            HireDate = form.HireDate!.Value,
            Position = form.Position,
            DepartmentId = form.DepartmentId!.Value
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Çalışan oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var response = await _employeeApi.GetByIdAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Çalışan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var form = new EmployeeFormViewModel
        {
            Id = response.Data.Id,
            FirstName = response.Data.FirstName,
            LastName = response.Data.LastName,
            NationalId = response.Data.NationalId,
            Email = response.Data.Email,
            Phone = response.Data.Phone,
            BirthDate = response.Data.BirthDate,
            HireDate = response.Data.HireDate,
            Position = response.Data.Position,
            DepartmentId = response.Data.DepartmentId,
            IsActive = response.Data.IsActive
        };

        await FillDepartmentOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        var response = await _employeeApi.UpdateAsync(form.Id, new UpdateEmployeeRequest
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            NationalId = form.NationalId,
            Email = form.Email,
            Phone = form.Phone,
            BirthDate = form.BirthDate!.Value,
            HireDate = form.HireDate!.Value,
            Position = form.Position,
            DepartmentId = form.DepartmentId!.Value,
            IsActive = form.IsActive
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Çalışan güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _employeeApi.DeleteAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Silme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "Çalışan silindi.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Departman dropdown'ının seçeneklerini API'den doldurur.
    /// Form her View'a dönüşünde çağrılmalı — aksi halde liste boş kalır.
    /// </summary>
    private async Task FillDepartmentOptionsAsync(EmployeeFormViewModel form)
    {
        var response = await _departmentApi.GetAllAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            // Dropdown doldurulamadı; sayfayı patlatmak yerine kullanıcıyı uyarıyoruz.
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
    }
}
