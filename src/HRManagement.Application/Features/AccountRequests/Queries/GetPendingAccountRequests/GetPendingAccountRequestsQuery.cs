using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Queries.GetPendingAccountRequests;

/// <summary>Bekleyen hesap taleplerini (kişi/talep-eden adlarıyla) döndürür. Admin ekranı.</summary>
public sealed record GetPendingAccountRequestsQuery : IRequest<IEnumerable<AccountRequestDto>>;
