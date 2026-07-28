using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetAllLeaveRequests;

public sealed class GetAllLeaveRequestsQueryHandler
    : IRequestHandler<GetAllLeaveRequestsQuery, IReadOnlyList<LeaveHistoryDto>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUserRepository _userRepository;

    public GetAllLeaveRequestsQueryHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IUserRepository userRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<LeaveHistoryDto>> Handle(
        GetAllLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        // Tüm şirketin izin geçmişi hassastır: yalnızca İK/Admin görebilir.
        // API rol kapısını burada da doğrularız — nihai otorite Application'dır.
        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actor is null || !actor.IsActive || actor.Role is not (Role.HR or Role.Admin))
            return [];

        var rows = await _leaveRequestRepository.GetAllWithNamesAsync();
        return rows.ToList();
    }
}
