using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;

/// <summary>
/// Bir çalışanın izin geçmişi. RequesterUserId imzalı JWT claim'inden gelir.
/// Yetki: kişi kendi kaydını; yöneticisi (zincirde yukarıda) astının kaydını;
/// HR/Admin herkesinkini görebilir. İzin verileri (hastalık dahil) hassastır,
/// bu yüzden yetki sorguya gömülüdür.
/// </summary>
public sealed record GetLeaveRequestsByEmployeeQuery(int EmployeeId, int RequesterUserId)
    : IRequest<IEnumerable<LeaveRequestDto>>;
