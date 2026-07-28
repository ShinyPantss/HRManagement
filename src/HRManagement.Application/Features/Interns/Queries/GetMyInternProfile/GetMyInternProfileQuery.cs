using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyInternProfile;

/// <summary>
/// "Profilim" (stajyer): isteği yapan hesabın stajyer kaydının profili.
/// Kimlik token'dan gelir; hesap bir stajyere bağlı değilse hata döner.
/// </summary>
public sealed record GetMyInternProfileQuery(int RequesterUserId)
    : IRequest<MyInternProfileDto>;
