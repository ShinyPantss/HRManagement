using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }

    // Talebi açan ya bir çalışan ya bir stajyerdir; tam olarak biri doludur.
    // (Veritabanı tarafında CK_LeaveRequests_Requester kısıtıyla garanti edilir.)
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }

    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }   // iş günü (hafta sonu hariç)
    public LeaveStatus Status { get; set; }
    public string? Description { get; set; }
    public string? MedicalReport { get; set; }
    public string? RejectionReason { get; set; }

    // İki aşamalı onayın izi.
    public int? ManagerApprovedByUserId { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public int? HrApprovedByUserId { get; set; }
    public DateTime? HrApprovedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
