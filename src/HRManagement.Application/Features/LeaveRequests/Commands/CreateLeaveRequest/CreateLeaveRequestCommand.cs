using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>
/// "Yeni izin talebi oluştur" isteği.
///
/// RequesterUserId, GİRİŞ YAPAN HESABIN kimliğidir ve controller tarafından
/// imzalı JWT claim'inden okunur — istek gövdesinden ASLA alınmaz (gövde
/// istemcinin elindedir, "ben başkasıyım" diyebilir). Talep her zaman kişinin
/// KENDİSİ için açılır (§5.3.1); hesabın bağlı olduğu çalışan/stajyer kaydını
/// handler çözer.
/// </summary>
public sealed record CreateLeaveRequestCommand(
    int RequesterUserId,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Description) : IRequest<int>;
