using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.LeaveRequests.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Services;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>
/// İki aşamalı onayın "ilerlet" tarafı:
///   Pending    → yönetici onayı işlenir → PendingHr
///   PendingHr  → İK onayı işlenir       → Approved
/// Yetki kuralları LeaveApprovalGuard'da (Reject ile ortak).
/// </summary>
public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly LeaveApprovalGuard _approvalGuard;

    public ApproveLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        LeaveApprovalGuard approvalGuard)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _approvalGuard = approvalGuard;
    }

    public async Task<Unit> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        var approver = await _approvalGuard.EnsureCanActAsync(leaveRequest, request.ApproverUserId);

        var now = DateTime.UtcNow;

        switch (leaveRequest.Status)
        {
            case LeaveStatus.Pending:
                // "Kontrol zamanı ≠ kullanım zamanı": talep açılırken bakiye
                // yetiyordu, ama aradan geçen sürede başka talepler onaylanmış
                // olabilir. Yıllık izinse burada TEKRAR denetlenir.
                if (leaveRequest.Type == LeaveType.Annual && leaveRequest.EmployeeId is int employeeId)
                    await EnsureBalanceStillSufficientAsync(employeeId);

                leaveRequest.ManagerApprovedByUserId = approver.Id;
                leaveRequest.ManagerApprovedAt = now;
                leaveRequest.Status = LeaveStatus.PendingHr;
                break;

            case LeaveStatus.PendingHr:
                leaveRequest.HrApprovedByUserId = approver.Id;
                leaveRequest.HrApprovedAt = now;
                leaveRequest.Status = LeaveStatus.Approved;
                break;
        }

        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        return Unit.Value;
    }

    // Bu talep zaten "kullanılan" toplamın İÇİNDE (bekleyenler rezerve sayılır),
    // bu yüzden koşul "used > limit" — talebi tekrar eklemeye gerek yok.
    private async Task EnsureBalanceStillSufficientAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            throw new ValidationException("Talep sahibi çalışan kaydı bulunamadı.");

        var today = DateTime.UtcNow.Date;
        var accrued = LeaveEntitlement.AccruedEntitlement(employee.HireDate, today, employee.AnnualLeaveDays);
        var nextGrant = LeaveEntitlement.NextGrant(employee.HireDate, today, employee.AnnualLeaveDays);

        var used = await _leaveRequestRepository.GetTotalUsedAnnualDaysAsync(employee.Id);

        if (used > accrued + nextGrant)
            throw new ValidationException(
                $"Talep açıldığından bu yana bakiye değişti: kullanılan+bekleyen {used} gün, " +
                $"izin verilen üst sınır {accrued + nextGrant} gün. Talep onaylanamaz.");
    }
}
