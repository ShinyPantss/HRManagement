using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Dashboard.Queries.GetHrDashboard;

public sealed class GetHrDashboardQueryHandler
    : IRequestHandler<GetHrDashboardQuery, HrDashboardDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public GetHrDashboardQueryHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<HrDashboardDto> Handle(GetHrDashboardQuery request, CancellationToken cancellationToken)
    {
        // Şirket geneli pano: yalnızca İK/Admin. API rol kapısını burada da doğrularız
        // (nihai otorite Application) — yetkisizse boş pano döner.
        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actor is null || !actor.IsActive || actor.Role is not (Role.HR or Role.Admin))
            return new HrDashboardDto();

        // "Bugün" diğer izin hesaplarıyla aynı kaynak: UTC tarih (LeaveEntitlement ile tutarlı).
        var today = DateTime.UtcNow.Date;

        var activeEmployees = (await _employeeRepository.GetAllAsync())
            .Where(e => e.IsActive)
            .ToList();

        var interns = await _internRepository.GetAllAsync();

        // İzinler isim + durum + tarih ile TEK sorguda; "şu an izinde" ve "bekleyen"
        // aynı listeden süzülür (çalışan + stajyer talepleri birlikte).
        var leaves = await _leaveRequestRepository.GetAllWithNamesAsync();

        var departments = await _departmentRepository.GetAllAsync();
        var departmentNames = departments.ToDictionary(d => d.Id, d => d.Name);

        var onLeave = leaves
            .Where(l => l.Status == LeaveStatus.Approved
                        && l.StartDate.Date <= today
                        && l.EndDate.Date >= today)
            .OrderBy(l => l.EndDate)
            .ToList();

        return new HrDashboardDto
        {
            TotalActiveEmployees = activeEmployees.Count,
            OnLeaveNowCount = onLeave.Count,
            PendingLeaveRequests = leaves.Count(l => l.Status is LeaveStatus.Pending or LeaveStatus.PendingHr),
            ActiveInterns = interns.Count(i => i.EndDate.Date >= today),

            MaleCount = activeEmployees.Count(e => e.Gender == Gender.Male),
            FemaleCount = activeEmployees.Count(e => e.Gender == Gender.Female),
            GenderUnspecifiedCount = activeEmployees.Count(e => e.Gender is null),

            DepartmentHeadcounts = activeEmployees
                .GroupBy(e => e.DepartmentId)
                .Select(g => new DepartmentHeadcountDto
                {
                    DepartmentName = departmentNames.TryGetValue(g.Key, out var n) ? n : "—",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DepartmentName)
                .ToList(),

            OnLeaveNow = onLeave
                .Select(l => new OnLeaveNowDto
                {
                    SubjectName = l.SubjectName,
                    SubjectType = l.SubjectType,
                    TypeName = l.TypeName,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate
                })
                .ToList()
        };
    }
}
