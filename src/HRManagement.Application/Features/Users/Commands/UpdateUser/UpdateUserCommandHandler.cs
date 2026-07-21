using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Input validation UpdateUserCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);

        if (user is null)
            throw new ValidationException("Kullanıcı bulunamadı.");

        var email = request.Email.Trim();

        // E-posta BAŞKA bir kullanıcıdaysa reddet. Kaydın kendi e-postasını
        // koruması serbest olmalı, bu yüzden Id karşılaştırması şart.
        var existingByEmail = await _userRepository.GetByEmailAsync(email);

        if (existingByEmail is not null && existingByEmail.Id != request.Id)
            throw new ValidationException("Bu e-posta zaten kullanılıyor.");

        user.Email = email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _userRepository.UpdateAsync(user);

        return Unit.Value;
    }
}
