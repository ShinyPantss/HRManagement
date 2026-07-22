using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. TotalDays entity'de tutulmaz;
/// başlangıç ve bitiş günleri dahil olacak şekilde burada hesaplanır.
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
        TotalDays = (leaveRequest.EndDate.Date - leaveRequest.StartDate.Date).Days + 1,
        Status = leaveRequest.Status,
        Description = leaveRequest.Description,
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
