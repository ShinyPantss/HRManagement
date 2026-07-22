using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.LeaveRequests.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>
/// İki aşamalı akışın "durdur" tarafı: talep hangi aşamadaysa, o aşamada
/// onaylamaya yetkili olan kişi reddedebilir (yetki: LeaveApprovalGuard).
/// Hangi aşamada reddedildiği ayrıca saklanmaz — ManagerApprovedAt doluysa
/// İK aşamasında, boşsa yönetici aşamasında reddedilmiştir.
/// </summary>
public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly LeaveApprovalGuard _approvalGuard;

    public RejectLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        LeaveApprovalGuard approvalGuard)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _approvalGuard = approvalGuard;
    }

    public async Task<Unit> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        var approver = await _approvalGuard.EnsureCanActAsync(leaveRequest, request.ApproverUserId);

        leaveRequest.Status = LeaveStatus.Rejected;
        leaveRequest.RejectionReason = request.Reason;
        leaveRequest.RejectedByUserId = approver.Id;
        leaveRequest.RejectedAt = DateTime.UtcNow;

        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        return Unit.Value;
    }
}
