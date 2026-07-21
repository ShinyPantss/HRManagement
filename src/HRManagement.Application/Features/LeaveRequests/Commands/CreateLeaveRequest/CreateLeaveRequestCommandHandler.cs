using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, int>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public CreateLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    // Input validation CreateLeaveRequestCommandValidator'da (tarih sırası dahil).
    public async Task<int> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            Status = LeaveStatus.Pending
        };

        return await _leaveRequestRepository.AddAsync(leaveRequest);
    }
}
