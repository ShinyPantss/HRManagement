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
/// İSTİSNA (kullanıcı kararı, 2026-07-23): talep sahibi HR rolündeyse yönetici
/// (HR müdürü) onayı YETER — talep doğrudan Approved olur. İkinci aşama "İK
/// onayı"dır ve İK'nın kendi talebinde o aşama kendi masasına düşerdi.
/// Yetki kuralları LeaveApprovalGuard'da (Reject ile ortak).
/// </summary>
public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly LeaveApprovalGuard _approvalGuard;

    public ApproveLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        LeaveApprovalGuard approvalGuard)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
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

                // HR çalışanının talebi yönetici onayıyla BİTER; İK aşaması
                // atlanır (HrApprovedBy boş kalır — denetim izi bunu gösterir).
                leaveRequest.Status = await IsOwnerHrAsync(leaveRequest)
                    ? LeaveStatus.Approved
                    : LeaveStatus.PendingHr;
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

    /// <summary>
    /// Talep sahibi HR rolünde bir çalışan mı? Rol JWT'den değil DB'den okunur
    /// (claim bayatlayabilir). Stajyer talepleri her zaman false döner.
    /// </summary>
    private async Task<bool> IsOwnerHrAsync(Domain.Entities.LeaveRequest leaveRequest)
    {
        if (leaveRequest.EmployeeId is not int employeeId)
            return false;

        var owner = await _employeeRepository.GetByIdAsync(employeeId);
        if (owner?.UserId is not int ownerUserId)
            return false;

        var ownerUser = await _userRepository.GetByIdAsync(ownerUserId);
        return ownerUser?.Role == Role.HR;
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
