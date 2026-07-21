using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>
/// "Yeni izin talebi oluştur" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. Talep her zaman Pending durumunda başlar.
/// </summary>
public sealed class CreateLeaveRequestCommand : IRequest<int>
{
    public CreateLeaveRequestCommand(
        int employeeId,
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        string? description)
    {
        EmployeeId = employeeId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
    }

    public int EmployeeId { get; }
    public LeaveType Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string? Description { get; }
}
