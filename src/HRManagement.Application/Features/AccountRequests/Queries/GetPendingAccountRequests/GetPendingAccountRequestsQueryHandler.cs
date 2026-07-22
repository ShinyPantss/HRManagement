using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Queries.GetPendingAccountRequests;

public sealed class GetPendingAccountRequestsQueryHandler
    : IRequestHandler<GetPendingAccountRequestsQuery, IEnumerable<AccountRequestDto>>
{
    private readonly IAccountRequestRepository _accountRequestRepository;

    public GetPendingAccountRequestsQueryHandler(IAccountRequestRepository accountRequestRepository)
    {
        _accountRequestRepository = accountRequestRepository;
    }

    public async Task<IEnumerable<AccountRequestDto>> Handle(
        GetPendingAccountRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _accountRequestRepository.GetPendingWithNamesAsync();
    }
}
