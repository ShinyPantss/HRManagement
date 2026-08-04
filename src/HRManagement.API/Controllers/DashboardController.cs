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

        var data = new HrDashboardResponse
        {
            TotalActiveEmployees = d.TotalActiveEmployees,
            OnLeaveNowCount = d.OnLeaveNowCount,
            PendingLeaveRequests = d.PendingLeaveRequests,
            ActiveInterns = d.ActiveInterns,

            OverduePendingCount = d.OverduePendingCount,
            OldestPendingDays = d.OldestPendingDays,
            EmployeesWithoutAccount = d.EmployeesWithoutAccount,
            InternsEndingSoon = d.InternsEndingSoon,

            // Eşikler handler'ın sabitlerinden okunur; ekran kendi sayısını uydurmaz.
            OverdueThresholdDays = GetHrDashboardQueryHandler.OverdueDays,
            UpcomingWindowDays = GetHrDashboardQueryHandler.UpcomingWindowDays,
            InternEndingWindowDays = GetHrDashboardQueryHandler.InternEndingWindowDays,

            MaleCount = d.MaleCount,
            FemaleCount = d.FemaleCount,
            GenderUnspecifiedCount = d.GenderUnspecifiedCount,

            SeniorityBreakdown = d.SeniorityBreakdown
                .Select(x => new SeniorityBreakdownResponse { Seniority = x.Seniority, Count = x.Count })
                .ToList(),

            OnLeaveNow = d.OnLeaveNow
                .Select(x => new OnLeaveNowResponse
                {
                    SubjectName = x.SubjectName,
                    SubjectType = x.SubjectType,
                    TypeName = x.TypeName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                })
                .ToList(),

            UpcomingLeaves = d.UpcomingLeaves
                .Select(x => new UpcomingLeaveResponse
                {
                    SubjectName = x.SubjectName,
                    SubjectType = x.SubjectType,
                    TypeName = x.TypeName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    WorkingDays = x.WorkingDays,
                    DaysUntilStart = x.DaysUntilStart
                })
                .ToList(),

            MonthlyTrend = d.MonthlyTrend
                .Select(x => new LeaveTrendPointResponse
                {
                    Year = x.Year,
                    Month = x.Month,
                    WorkingDays = x.WorkingDays,
                    RequestCount = x.RequestCount
                })
                .ToList(),

            YearlyPersonTrend = d.YearlyPersonTrend
                .Select(x => new MonthlyPersonCountResponse
                {
                    Year = x.Year,
                    Month = x.Month,
                    PersonCount = x.PersonCount
                })
                .ToList()
        };

        return Ok(BaseResponse<HrDashboardResponse>.Success(data));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
