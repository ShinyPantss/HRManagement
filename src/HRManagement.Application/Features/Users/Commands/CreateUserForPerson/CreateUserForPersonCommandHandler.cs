using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.CreateUserForPerson;

public sealed class CreateUserForPersonCommandHandler : IRequestHandler<CreateUserForPersonCommand, int>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserForPersonCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
        _passwordHasher = passwordHasher;
    }

    // Input validation CreateUserForPersonCommandValidator'da.
    // Buradaki kurallar DB'ye bakar: kişi var mı, zaten hesabı var mı, isim/e-posta benzersiz mi.
    public async Task<int> Handle(CreateUserForPersonCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        // 1) Hesap açılacak kişi gerçekten var mı ve zaten hesabı YOK mu?
        await EnsurePersonNeedsAccountAsync(request.EmployeeId, request.InternId);

        // 2) Hesap benzersizliği (Users tablosundaki UNIQUE kısıtların ön kontrolü).
        if (await _userRepository.GetByUsernameAsync(username) is not null)
            throw new ValidationException("Bu kullanıcı adı zaten kullanılıyor.");

        if (await _userRepository.GetByEmailAsync(email) is not null)
            throw new ValidationException("Bu e-posta ile bir hesap zaten var.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };

        // Hesabı oluştur VE kişiye bağla — tek transaction (repo içinde).
        return await _userRepository.CreateForPersonAsync(user, request.EmployeeId, request.InternId);
    }

    private async Task EnsurePersonNeedsAccountAsync(int? employeeId, int? internId)
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
