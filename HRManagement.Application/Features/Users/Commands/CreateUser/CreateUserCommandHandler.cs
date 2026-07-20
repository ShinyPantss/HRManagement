using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ValidationException("Kullanıcı adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("E-posta zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Şifre zorunludur.");

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        var existingByUsername = await _userRepository.GetByUsernameAsync(username);

        if (existingByUsername is not null)
            throw new ValidationException("Bu kullanıcı adı zaten kullanılıyor.");

        // E-posta benzersizliği de zorunlu: aksi halde aynı adrese birden fazla
        // hesap açılabilir ve e-posta ile giriş hangi hesabı açtığı belirsizleşir.
        var existingByEmail = await _userRepository.GetByEmailAsync(email);

        if (existingByEmail is not null)
            throw new ValidationException("Bu e-posta zaten kullanılıyor.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };

        return await _userRepository.AddAsync(user);
    }
}
