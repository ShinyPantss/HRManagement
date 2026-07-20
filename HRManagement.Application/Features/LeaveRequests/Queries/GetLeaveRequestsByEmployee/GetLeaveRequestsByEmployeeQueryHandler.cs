using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;

public sealed class GetLeaveRequestsByEmployeeQueryHandler : IRequestHandler<GetLeaveRequestsByEmployeeQuery, IEnumerable<LeaveRequestDto>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetLeaveRequestsByEmployeeQueryHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<IEnumerable<LeaveRequestDto>> Handle(GetLeaveRequestsByEmployeeQuery request, CancellationToken cancellationToken)
    {
        var leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(request.EmployeeId);
        return leaveRequests.Select(LeaveRequestMapping.ToDto);
    }
}
