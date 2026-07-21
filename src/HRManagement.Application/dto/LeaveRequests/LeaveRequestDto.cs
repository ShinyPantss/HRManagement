using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
}