using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;

public sealed class ApproveAccountRequestCommandHandler : IRequestHandler<ApproveAccountRequestCommand, int>
{
    private readonly IAccountRequestRepository _accountRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ApproveAccountRequestCommandHandler(
        IAccountRequestRepository accountRequestRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRequestRepository = accountRequestRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(ApproveAccountRequestCommand request, CancellationToken cancellationToken)
    {
        var accountRequest = await _accountRequestRepository.GetByIdAsync(request.Id);

        if (accountRequest is null)
            throw new ValidationException("Hesap talebi bulunamadı.");

        if (accountRequest.Status != AccountRequestStatus.Pending)
            throw new ValidationException("Sadece bekleyen talepler onaylanabilir.");

        // Talep açıldığından beri kişiye doğrudan hesap açılmış olabilir (yarış).
        await EnsureSubjectStillNeedsAccountAsync(accountRequest);

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _userRepository.GetByUsernameAsync(username) is not null)
            throw new ValidationException("Bu kullanıcı adı zaten kullanılıyor.");

        if (await _userRepository.GetByEmailAsync(email) is not null)
            throw new ValidationException("Bu e-posta ile bir hesap zaten var.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            // Admin rolü değiştirmezse HR'ın önerisi geçerli.
            Role = request.Role ?? accountRequest.SuggestedRole,
            IsActive = true
        };

        // Hesap oluştur + kişiye bağla + talebi Onaylandı yap — TEK transaction.
        return await _userRepository.CreateForPersonAsync(
            user, accountRequest.EmployeeId, accountRequest.InternId,
            accountRequestId: accountRequest.Id, reviewerUserId: request.ApproverUserId);
    }

    private async Task EnsureSubjectStillNeedsAccountAsync(AccountRequest accountRequest)
    {
        if (accountRequest.EmployeeId is int eid)
        {
            var employee = await _employeeRepository.GetByIdAsync(eid);
            if (employee is null)
                throw new ValidationException("Çalışan bulunamadı.");
            if (employee.UserId is not null)
                throw new ValidationException("Bu çalışana bu arada bir hesap açılmış.");
        }
        else if (accountRequest.InternId is int iid)
        {
            var intern = await _internRepository.GetByIdAsync(iid);
            if (intern is null)
                throw new ValidationException("Stajyer bulunamadı.");
            if (intern.UserId is not null)
                throw new ValidationException("Bu stajyere bu arada bir hesap açılmış.");
        }
    }
}
