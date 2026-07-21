using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommand : IRequest<Unit>
{
    public DeleteLeaveRequestCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
