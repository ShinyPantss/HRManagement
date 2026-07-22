using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.CreateAccountRequest;

public sealed class CreateAccountRequestCommandHandler : IRequestHandler<CreateAccountRequestCommand, int>
{
    private readonly IAccountRequestRepository _accountRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public CreateAccountRequestCommandHandler(
        IAccountRequestRepository accountRequestRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _accountRequestRepository = accountRequestRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    // Input validation validator'da. Buradaki kurallar DB'ye bakar.
    public async Task<int> Handle(CreateAccountRequestCommand request, CancellationToken cancellationToken)
    {
        // Kişi GERÇEKTEN var mı ve zaten hesabı YOK mu? (FK varlığı garanti eder
        // ama anlaşılır mesaj için önden kontrol ederiz.)
        await EnsureSubjectNeedsAccountAsync(request.EmployeeId, request.InternId);

        // Aynı kişiye bekleyen bir talep zaten var mı? (DB'de filtered unique index
        // de var; buradaki kontrol kullanıcıya net mesaj verir.)
        if (await _accountRequestRepository.HasPendingAsync(request.EmployeeId, request.InternId))
            throw new ValidationException("Bu kişi için zaten bekleyen bir hesap talebi var.");

        var accountRequest = new AccountRequest
        {
            EmployeeId = request.EmployeeId,
            InternId = request.InternId,
            RequestedByUserId = request.RequestedByUserId,
            SuggestedRole = request.SuggestedRole,
            Note = request.Note,
            Status = AccountRequestStatus.Pending
        };

        return await _accountRequestRepository.AddAsync(accountRequest);
    }

    private async Task EnsureSubjectNeedsAccountAsync(int? employeeId, int? internId)
    {
        if (employeeId is int eid)
        {
            var employee = await _employeeRepository.GetByIdAsync(eid);
            if (employee is null)
                throw new ValidationException("Çalışan bulunamadı.");
            if (employee.UserId is not null)
                throw new ValidationException("Bu çalışanın zaten bir giriş hesabı var.");
        }
        else if (internId is int iid)
        {
            var intern = await _internRepository.GetByIdAsync(iid);
            if (intern is null)
                throw new ValidationException("Stajyer bulunamadı.");
            if (intern.UserId is not null)
                throw new ValidationException("Bu stajyerin zaten bir giriş hesabı var.");
        }
    }
}
