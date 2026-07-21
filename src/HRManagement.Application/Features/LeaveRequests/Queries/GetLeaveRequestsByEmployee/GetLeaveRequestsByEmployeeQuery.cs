using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;

public sealed record GetLeaveRequestsByEmployeeQuery(int EmployeeId) : IRequest<IEnumerable<LeaveRequestDto>>;
