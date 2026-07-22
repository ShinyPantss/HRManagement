using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.RejectAccountRequest;

public sealed class RejectAccountRequestCommandHandler : IRequestHandler<RejectAccountRequestCommand, Unit>
{
    private readonly IAccountRequestRepository _accountRequestRepository;

    public RejectAccountRequestCommandHandler(IAccountRequestRepository accountRequestRepository)
    {
        _accountRequestRepository = accountRequestRepository;
    }

    public async Task<Unit> Handle(RejectAccountRequestCommand request, CancellationToken cancellationToken)
    {
        var accountRequest = await _accountRequestRepository.GetByIdAsync(request.Id);

        if (accountRequest is null)
            throw new ValidationException("Hesap talebi bulunamadı.");

        if (accountRequest.Status != AccountRequestStatus.Pending)
            throw new ValidationException("Sadece bekleyen talepler reddedilebilir.");

        accountRequest.Status = AccountRequestStatus.Rejected;
        accountRequest.RejectionReason = request.Reason;
        accountRequest.ReviewedByUserId = request.ApproverUserId;
        accountRequest.ReviewedAt = DateTime.UtcNow;

        await _accountRequestRepository.UpdateAsync(accountRequest);

        return Unit.Value;
    }
}
