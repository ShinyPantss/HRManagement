using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRManagement.WebUI.Models;
using HRManagement.WebUI.Models.Api.Dashboard;
using HRManagement.WebUI.Models.Home;
using HRManagement.WebUI.Services;

namespace HRManagement.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardApi _dashboardApi;
    private readonly IEmployeeApi _employeeApi;
    private readonly ILeaveRequestApi _leaveRequestApi;
    private readonly IInternApi _internApi;

    public HomeController(
        IDashboardApi dashboardApi,
        IEmployeeApi employeeApi,
        ILeaveRequestApi leaveRequestApi,
        IInternApi internApi)
    {
        _dashboardApi = dashboardApi;
        _employeeApi = employeeApi;
        _leaveRequestApi = leaveRequestApi;
        _internApi = internApi;
    }

    public async Task<IActionResult> Index()
    {
        // İK/Admin → şirket geneli pano (kendi API ucu var).
        if (User.IsInRole("HR") || User.IsInRole("Admin"))
        {
            var response = await _dashboardApi.GetHrDashboardAsync();

            if (!response.IsSuccess || response.Data is null)
                TempData["Error"] = response.Message ?? "Pano verileri alınamadı.";

            return View("Dashboard", response.Data ?? new HrDashboardResponse());
        }

        // Stajyerin ana sayfası "Staj Panelim": ilerleme + görevler + izinler.
        // Profilim'e YÖNLENDİRMİYORUZ — o ayrı bir ekran (kimlik künyesi).
        if (User.IsInRole("Intern"))
        {
            var profile = await _internApi.GetMyProfileAsync();

            if (!profile.IsSuccess || profile.Data is null)
            {
                TempData["Error"] = profile.Message ?? "Staj bilgileriniz alınamadı.";
                return View("InternHome", new InternHomeViewModel());
            }

            var tasks = await _internApi.GetMyTasksAsync();

            return View("InternHome", new InternHomeViewModel
            {
                Profile = profile.Data,
                Tasks = tasks.Data?.Tasks ?? [],
                MentorFullName = tasks.Data?.MentorFullName ?? profile.Data.MentorFullName
            });
        }

        // Yönetici ve çalışan → kişisel pano. Yeni bir API ucu YOK: var olan
        // /me ve /pending-approvals uçları birleştiriliyor.
        var model = new PersonalHomeViewModel
        {
            CanApprove = User.IsInRole("Manager")
        };

        var me = await _employeeApi.GetMyProfileAsync();

        if (!me.IsSuccess || me.Data is null)
            TempData["Error"] = me.Message ?? "Profil bilgileriniz alınamadı.";
        else
            model.Me = me.Data;

        if (model.CanApprove)
        {
            var pending = await _leaveRequestApi.GetPendingApprovalsAsync();
            if (pending.IsSuccess)
                model.PendingApprovals = pending.Data ?? [];
        }

        return View(model);
    }

    // Hata sayfası girişsiz de açılabilmeli: aksi halde giriş yapılmamışken oluşan
    // bir hata login'e yönlenir, orada da hata olursa döngüye girilir.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
