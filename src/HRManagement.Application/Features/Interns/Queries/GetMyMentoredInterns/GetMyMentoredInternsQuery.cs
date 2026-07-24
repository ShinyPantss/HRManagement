using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyMentoredInterns;

/// <summary>
/// "Mentorluk" listesi: isteği yapan hesabın çalışan kaydına bağlı stajyerlar.
/// Kimlik token'dan gelir; hesap bir çalışana bağlı değilse veya kimseye
/// mentorluk yapmıyorsa boş liste döner (hata değil — ekran boş durum gösterir).
/// </summary>
public sealed record GetMyMentoredInternsQuery(int RequesterUserId)
    : IRequest<IEnumerable<InternDto>>;
