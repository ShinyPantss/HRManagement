using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyInternTasks;

/// <summary>
/// "Görevlerim": isteği yapan hesabın stajyer kaydına atanmış görevler.
/// Kimlik token'dan gelir — stajyer yalnızca KENDİ görevlerini görebilir.
/// </summary>
public sealed record GetMyInternTasksQuery(int RequesterUserId)
    : IRequest<MyInternTasksDto>;
