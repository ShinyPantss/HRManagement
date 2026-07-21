using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public ApproveLeaveRequestCommandHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Unit> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        if (leaveRequest.Status != LeaveStatus.Pending)
            throw new ValidationException("Sadece bekleyen talepler onaylanabilir.");

        leaveRequest.Status = LeaveStatus.Approved;

        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        return Unit.Value;
    }
}
