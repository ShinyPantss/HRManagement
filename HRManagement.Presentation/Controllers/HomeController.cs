using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Presentation.Controllers;

public class HomeController : Controller
{
    // Auth bağlandığında: kullanıcının rolüne göre ilgili dashboard
    // verilerini (IEmployeeService, ILeaveRequestService...) çekecek.
    public IActionResult Index()
    {
        return View();
    }
}
