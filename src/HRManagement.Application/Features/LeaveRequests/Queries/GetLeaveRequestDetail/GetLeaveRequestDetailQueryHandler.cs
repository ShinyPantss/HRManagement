using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.LeaveRequests.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestDetail;

public sealed class GetLeaveRequestDetailQueryHandler
    : IRequestHandler<GetLeaveRequestDetailQuery, LeaveDetailDto>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;
    private readonly LeaveApprovalGuard _approvalGuard;

    public GetLeaveRequestDetailQueryHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository,
        LeaveApprovalGuard approvalGuard)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
        _approvalGuard = approvalGuard;
    }

    public async Task<LeaveDetailDto> Handle(GetLeaveRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var leave = await _leaveRequestRepository.GetByIdAsync(request.Id);
        if (leave is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        // Kişi (sahip) + tip çözümü — ve sahip hesabı (kendi kaydını görebilmesi için).
        var (subjectName, subjectType, ownerUserId) = await ResolveSubjectAsync(leave);

        // "Şu an işlem yapabilir mi?" kararını, onay/red ile AYNI guard'a sordurarak
        // üretiriz — buton, gerçekte kabul edilecek işlemi birebir yansıtsın (kural kopyası yok).
        // Guard yetkisizlikte ValidationException atar; burada bunu bool'a çeviririz.
        var canActNow = await CanActNowAsync(leave, actor.Id);

        // Görüntüleme yetkisi: HR/Admin herkesi; aksi hâlde sahip ya da işleme yetkili.
        var isHrAdmin = actor.Role is Role.HR or Role.Admin;
        if (!isHrAdmin && ownerUserId != actor.Id && !canActNow)
            throw new ValidationException("Bu izin kaydını görüntüleme yetkiniz yok.");

        return new LeaveDetailDto
        {
            Id = leave.Id,
            SubjectName = subjectName,
            SubjectType = subjectType,
            TypeName = leave.Type.ToString(),
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            WorkingDays = leave.WorkingDays,
            Status = leave.Status,
            Description = leave.Description,
            MedicalReport = leave.MedicalReport,
            RejectionReason = leave.RejectionReason,
            CreatedAt = leave.CreatedAt,
            ManagerApprovedByName = await ResolveActorNameAsync(leave.ManagerApprovedByUserId),
            ManagerApprovedAt = leave.ManagerApprovedAt,
            HrApprovedByName = await ResolveActorNameAsync(leave.HrApprovedByUserId),
            HrApprovedAt = leave.HrApprovedAt,
            RejectedByName = await ResolveActorNameAsync(leave.RejectedByUserId),
            RejectedAt = leave.RejectedAt,
            CanActNow = canActNow
        };
    }

    private async Task<bool> CanActNowAsync(LeaveRequest leave, int actorUserId)
    {
        try
        {
            await _approvalGuard.EnsureCanActAsync(leave, actorUserId);
            return true;
        }
        catch (ValidationException)
        {
            // İşlem bekleyen aşamada değil ya da yetki yok — ikisi de "işlem yapamaz".
            return false;
        }
    }

    private async Task<(string Name, string Type, int? OwnerUserId)> ResolveSubjectAsync(LeaveRequest leave)
    {
        if (leave.EmployeeId is int employeeId)
        {
            var e = await _employeeRepository.GetByIdAsync(employeeId);
            return (e is null ? "—" : $"{e.FirstName} {e.LastName}", "Çalışan", e?.UserId);
        }

        if (leave.InternId is int internId)
        {
            var i = await _internRepository.GetByIdAsync(internId);
            return (i is null ? "—" : $"{i.FirstName} {i.LastName}", "Stajyer", i?.UserId);
        }

        return ("—", "—", null);
    }

    /// <summary>
    /// Onaylayan/reddeden hesabın gösterim adı: önce bağlı çalışanın adı-soyadı,
    /// yoksa kullanıcı adı (İK/Admin çalışan kaydı olmadan da işlem yapabilir).
    /// </summary>
    private async Task<string?> ResolveActorNameAsync(int? userId)
    {
        if (userId is not int id)
            return null;

        var employee = await _employeeRepository.GetByUserIdAsync(id);
        if (employee is not null)
            return $"{employee.FirstName} {employee.LastName}";

        var user = await _userRepository.GetByIdAsync(id);
        return user?.Username;
    }
}
