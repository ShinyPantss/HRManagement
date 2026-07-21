using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed record DeleteLeaveRequestCommand(int Id) : IRequest<Unit>;
