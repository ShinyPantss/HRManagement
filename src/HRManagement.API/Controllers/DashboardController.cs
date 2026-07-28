using System.Security.Claims;
using HRManagement.API.Models;
using HRManagement.API.Models.Dashboard;
using HRManagement.Application.Features.Dashboard.Queries.GetHrDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // İK/Admin ana sayfa panosu: şirket geneli özet metrikler. Rol kapısı burada;
    // handler ayrıca aktörün rolünü doğrular (Application otorite).
    [Authorize(Roles = "HR,Admin")]
    [HttpGet("hr")]
    public async Task<IActionResult> GetHrDashboard()
    {
        var d = await _mediator.Send(new GetHrDashboardQuery(CurrentUserId()));

        var data = new HrDashboardResponse(
            d.TotalActiveEmployees, d.OnLeaveNowCount, d.PendingLeaveRequests, d.ActiveInterns,
            d.MaleCount, d.FemaleCount, d.GenderUnspecifiedCount,
            d.DepartmentHeadcounts
                .Select(x => new DepartmentHeadcountResponse(x.DepartmentName, x.Count)).ToList(),
            d.OnLeaveNow
                .Select(x => new OnLeaveNowResponse(
                    x.SubjectName, x.SubjectType, x.TypeName, x.StartDate, x.EndDate)).ToList());

        return Ok(BaseResponse<HrDashboardResponse>.Success(data));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
