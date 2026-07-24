using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetPendingApprovals;

public sealed class GetPendingApprovalsQueryHandler
    : IRequestHandler<GetPendingApprovalsQuery, IReadOnlyList<PendingApprovalDto>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetPendingApprovalsQueryHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<PendingApprovalDto>> Handle(
        GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actor is null || !actor.IsActive)
            return [];

        var isAdmin = actor.Role == Role.Admin;
        var isHr = actor.Role == Role.HR;

        // Actor'ın çalışan kaydı + ekibi (yönetici aşaması için). Ekip = zincirde
        // AŞAĞIDAKİLER (GetTeamAsync). Hesabı çalışana bağlı değilse ekip boştur.
        var actorEmployee = await _employeeRepository.GetByUserIdAsync(actor.Id);
        var teamIds = new HashSet<int>();
        if (actorEmployee is not null)
            foreach (var member in await _employeeRepository.GetTeamAsync(actorEmployee.Id))
                teamIds.Add(member.Id);

        var candidates = await _leaveRequestRepository.GetActionableWithNamesAsync();

        // Yetki süzmesi LeaveApprovalGuard ile AYNI kuralı yansıtır (yazma yolu =
        // okuma yolu tutarlı olsun):
        //   Pending    → Admin her şeyi; aksi hâlde talep sahibi ekibimde (çalışan)
        //                ya da mentoru ben/ekibimden biri (stajyer).
        //   PendingHr  → İK/Admin VE 1. aşamayı ben ONAYLAMADIYSAM (iki ayrı göz).
        //   Her aşamada kendi talebim hariç.
        var result = new List<PendingApprovalDto>();
        foreach (var c in candidates)
        {
            if (c.OwnerUserId == actor.Id)
                continue;

            var canAct = c.Status switch
            {
                LeaveStatus.Pending =>
                    isAdmin
                    || (c.EmployeeId is int eid && teamIds.Contains(eid))
                    || (c.InternId is not null && c.MentorId is int mid
                        && (mid == actorEmployee?.Id || teamIds.Contains(mid))),

                LeaveStatus.PendingHr =>
                    (isHr || isAdmin) && c.ManagerApprovedByUserId != actor.Id,

                _ => false
            };

            if (canAct)
                result.Add(c);
        }

        return result;
    }
}
