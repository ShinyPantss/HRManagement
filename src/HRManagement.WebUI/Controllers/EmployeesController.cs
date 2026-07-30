using HRManagement.WebUI.Models;
using HRManagement.WebUI.Models.Api.Employees;
using HRManagement.WebUI.Models.Employees;
using HRManagement.WebUI.Models.Units;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IUnitApi _unitApi;
    private readonly IInternApi _internApi;

    public EmployeesController(
        IEmployeeApi employeeApi,
        IDepartmentApi departmentApi,
        IUnitApi unitApi,
        IInternApi internApi)
    {
        _employeeApi = employeeApi;
        _departmentApi = departmentApi;
        _unitApi = unitApi;
        _internApi = internApi;
    }

    /// <param name="departmentId">
    /// Departmanlar ekranındaki "Çalışanları gör" bağlantısından gelir; listeyi
    /// süzmez, yalnızca istemci tarafı filtre çubuğunda o departmanı SEÇİLİ açar.
    /// Süzme işini var olan filtre scripti yapar — ikinci bir filtreleme yolu
    /// açmamak için liste sunucuda daraltılmıyor.
    /// Geçersiz bir id gelirse eşleşen seçenek olmaz ve filtre "Tümü"de kalır.
    /// </param>
    public async Task<IActionResult> Index(int? departmentId = null)
    {
        ViewBag.SelectedDepartmentId = departmentId;

        var response = await _employeeApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message;
            return View(new List<EmployeeResponse>());
        }

        // "Pozisyon" = (Birim varsa Birim, yoksa Departman) + Kıdem (türetilir).
        // Liste için departman + birim Id→Ad sözlüklerini hazırlayıp View'a veriyoruz.
        var departments = await _departmentApi.GetAllAsync();
        ViewBag.DepartmentNames = (departments.Data ?? [])
            .ToDictionary(d => d.Id, d => d.Name);

        var units = await _unitApi.GetAllAsync();
        ViewBag.UnitNames = (units.Data ?? [])
            .ToDictionary(u => u.Id, u => u.Name);
        // Birim filtresi departmana göre süzülebilsin diye tam liste de gider.
        ViewBag.Units = units.Data ?? [];

        if (User.IsInRole("HR") || User.IsInRole("Admin"))
        {
            // Stajyer aç/kapa filtresi: açıkken stajyerlar kendi birimlerinin
            // altında listelenir. Yalnızca HR/Admin — diğer rollerin stajyer
            // listesine erişimi zaten kapalı.
            var interns = await _internApi.GetAllAsync();
            ViewBag.Interns = interns.Data ?? [];
        }
        else
        {
            // Ekibim görünümü (Manager/Employee): listeyi "Yöneticim" ve "Ekip
            // Arkadaşlarım" diye bölebilmek için kişinin kendi kaydını öğreniyoruz.
            var me = await _employeeApi.GetMyProfileAsync();
            ViewBag.MyEmployeeId = me.Data?.Id;
            ViewBag.MyManagerId = me.Data?.ManagerId;
        }

        return View(response.Data ?? new List<EmployeeResponse>());
    }

    // Detay sayfası. Rol attribute'u YOK: kimin kimi görebileceğine API karar
    // verir (Employee başkasının id'sini yazarsa API reddeder, biz mesajı gösteririz).
    public async Task<IActionResult> Details(int id)
    {
        var response = await _employeeApi.GetDetailAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Çalışan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    // "Profilim": kişinin KENDİ detay sayfası — aynı view, veri /me ucundan.
    // Hesap bir çalışan kaydına bağlı değilse (ör. Admin) API 404 döner.
    public async Task<IActionResult> Profile()
    {
        var response = await _employeeApi.GetMyProfileAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Hesabınız bir çalışan kaydına bağlı değil.";
            return RedirectToAction("Index", "Home");
        }

        // "İzin Talep Et" gibi yalnızca kişinin kendi profilinde anlamlı
        // öğeleri view bu bayrakla açar.
        ViewData["IsOwnProfile"] = true;
        return View(nameof(Details), response.Data);
    }

    // Not ekleme (§5.2). Rol attribute'u UX içindir; Manager'ın "yalnızca kendi
    // ekibi" kısıtı dahil son söz API + Application'dadır.
    [HttpPost]
    [Authorize(Roles = "HR,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Not içeriği boş olamaz.";
        }
        else
        {
            var response = await _employeeApi.AddNoteAsync(id, new AddEmployeeNoteRequest
            {
                Content = content.Trim()
            });

            if (response.IsSuccess)
                TempData["Success"] = response.Message ?? "Not eklendi.";
            else
                TempData["Error"] = response.Message ?? "Not eklenemedi.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // Çalışan kaydı AÇMAK yalnızca İK işidir; Admin dahil kimse açamaz
    // (kullanıcı kararı). Düzenleme HR+Admin'de kalır.
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create()
    {
        var form = new EmployeeFormViewModel
        {
            IsActive = true,
            // Boş date input'ta takvim bugünden açılır; doğum tarihi için
            // 2000'den başlatmak seçim yapmayı kolaylaştırır.
            BirthDate = new DateTime(2000, 1, 1)
        };
        await FillOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // Form geri gösterilecekse dropdown'lar yeniden doldurulmalı;
            // seçenekler post gövdesiyle geri gelmez.
            await FillOptionsAsync(form);
            return View(form);
        }

        var response = await _employeeApi.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            NationalId = form.NationalId,
            Email = form.Email,
            Phone = form.Phone,
            Gender = form.Gender,
            // ModelState geçerliyse [Required] sayesinde bu alanlar dolu.
            BirthDate = form.BirthDate!.Value,
            HireDate = form.HireDate!.Value,
            DepartmentId = form.DepartmentId!.Value,
            UserId = form.UserId,
            ManagerId = form.ManagerId,
            Seniority = form.Seniority,
            AnnualLeaveDays = form.AnnualLeaveDays,
            UnitId = form.UnitId,
            RequestLoginAccount = form.RequestLoginAccount
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Çalışan oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "HR,Admin")]
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
            Gender = response.Data.Gender,
            BirthDate = response.Data.BirthDate,
            HireDate = response.Data.HireDate,
            DepartmentId = response.Data.DepartmentId,
            UserId = response.Data.UserId,
            ManagerId = response.Data.ManagerId,
            Seniority = response.Data.Seniority,
            AnnualLeaveDays = response.Data.AnnualLeaveDays,
            UnitId = response.Data.UnitId,
            IsActive = response.Data.IsActive
        };

        await FillOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [Authorize(Roles = "HR,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        var response = await _employeeApi.UpdateAsync(form.Id, new UpdateEmployeeRequest
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            NationalId = form.NationalId,
            Email = form.Email,
            Phone = form.Phone,
            Gender = form.Gender,
            BirthDate = form.BirthDate!.Value,
            HireDate = form.HireDate!.Value,
            DepartmentId = form.DepartmentId!.Value,
            UserId = form.UserId,
            ManagerId = form.ManagerId,
            Seniority = form.Seniority,
            AnnualLeaveDays = form.AnnualLeaveDays,
            UnitId = form.UnitId,
            IsActive = form.IsActive
        });

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            await FillOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Çalışan güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    /// Departman ve yönetici dropdown seçeneklerini API'den doldurur.
    /// Form her View'a dönüşünde çağrılmalı — aksi halde listeler boş kalır.
    /// </summary>
    private async Task FillOptionsAsync(EmployeeFormViewModel form)
    {
        var departments = await _departmentApi.GetAllAsync();

        if (!departments.IsSuccess || departments.Data is null)
        {
            // Dropdown doldurulamadı; sayfayı patlatmak yerine kullanıcıyı uyarıyoruz.
            TempData["Error"] = departments.Message ?? "Departman listesi alınamadı.";
            form.DepartmentOptions = [];
        }
        else
        {
            form.DepartmentOptions = departments.Data
                .Select(department => new SelectListItem
                {
                    Text = department.Name,
                    Value = department.Id.ToString()
                })
                .ToList();
        }

        // Yönetici adayları = YALNIZCA yönetici kademesindeki (GM/GMY/Müdür)
        // çalışanlar; kişinin kendisi listelenmez. Kıdemce "çalışandan yüksek"
        // süzmesini JS seçilen kıdeme göre yapar; API her hâlükârda son sözü söyler.
        var employees = await _employeeApi.GetAllAsync();

        form.ManagerCandidates = (employees.Data ?? [])
            .Where(e => e.Id != form.Id && SeniorityDisplay.IsManagerial(e.Seniority))
            .Select(e => new ManagerCandidate(e.Id, $"{e.FirstName} {e.LastName}", e.Seniority, e.DepartmentId))
            .ToList();

        // Tüm birimler; view'daki dropdown seçilen departmana göre JS ile süzülür.
        var units = await _unitApi.GetAllAsync();
        form.UnitCandidates = (units.Data ?? [])
            .Select(u => new UnitOption(u.Id, u.Name, u.DepartmentId))
            .ToList();
    }
}
