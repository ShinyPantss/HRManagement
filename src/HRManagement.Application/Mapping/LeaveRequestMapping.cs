using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. TotalDays artık İŞ GÜNÜ sayısıdır ve
/// oluşturulurken hesaplanıp entity'de saklanır (WorkingDays).
/// </summary>
public static class LeaveRequestMapping
{
    public static LeaveRequestDto ToDto(LeaveRequest leaveRequest) => new()
    {
        Id = leaveRequest.Id,
        EmployeeId = leaveRequest.EmployeeId,
        InternId = leaveRequest.InternId,
        Type = leaveRequest.Type,
        StartDate = leaveRequest.StartDate,
        EndDate = leaveRequest.EndDate,
        TotalDays = leaveRequest.WorkingDays,   // iş günü (hafta sonu hariç)
        Status = leaveRequest.Status,
        Description = leaveRequest.Description,
        MedicalReport = leaveRequest.MedicalReport,
        RejectionReason = leaveRequest.RejectionReason,
        ManagerApprovedByUserId = leaveRequest.ManagerApprovedByUserId,
        ManagerApprovedAt = leaveRequest.ManagerApprovedAt,
        HrApprovedByUserId = leaveRequest.HrApprovedByUserId,
        HrApprovedAt = leaveRequest.HrApprovedAt,
        RejectedByUserId = leaveRequest.RejectedByUserId,
        RejectedAt = leaveRequest.RejectedAt,
        CreatedAt = leaveRequest.CreatedAt
    };
}
