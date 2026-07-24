using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMentoredInternDetail;

/// <summary>
/// Mentorun stajyer detay sayfası: temel bilgi + görevler + mentor notları.
/// Yalnızca stajyerin MENTORU açabilir (MentorshipGuard) — HR'ın stajyer
/// yönetim ekranı bundan ayrıdır.
/// </summary>
public sealed record GetMentoredInternDetailQuery(int InternId, int RequesterUserId)
    : IRequest<MentoredInternDetailDto>;
