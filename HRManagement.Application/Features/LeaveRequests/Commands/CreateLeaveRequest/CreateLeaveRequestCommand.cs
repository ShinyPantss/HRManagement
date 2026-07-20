using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>
/// "Yeni izin talebi oluştur" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. Talep her zaman Pending durumunda başlar.
/// </summary>
public sealed record CreateLeaveRequestCommand(
    int EmployeeId,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Description) : IRequest<int>;
