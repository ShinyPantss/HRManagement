using HRManagement.Domain.Enums;

namespace HRManagement.Domain.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? RejectionReason { get; set; }
}