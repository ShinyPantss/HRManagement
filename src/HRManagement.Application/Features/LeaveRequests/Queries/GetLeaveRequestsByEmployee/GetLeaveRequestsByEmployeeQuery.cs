using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;

public sealed class GetLeaveRequestsByEmployeeQuery : IRequest<IEnumerable<LeaveRequestDto>>
{
    public GetLeaveRequestsByEmployeeQuery(int employeeId)
    {
        EmployeeId = employeeId;
    }

    public int EmployeeId { get; }
}
