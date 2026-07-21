using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public RejectLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Unit> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        if (leaveRequest.Status != LeaveStatus.Pending)
            throw new ValidationException("Sadece bekleyen talepler reddedilebilir.");

        leaveRequest.Status = LeaveStatus.Rejected;
        leaveRequest.RejectionReason = request.Reason;

        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        return Unit.Value;
    }
}
