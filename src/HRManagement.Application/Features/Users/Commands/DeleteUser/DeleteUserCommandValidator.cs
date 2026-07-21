using FluentValidation;

namespace HRManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Input validation. Id'nin kendi içinde anlamlı olup olmadığına bakar.
/// "Kullanıcı bulunamadı." kontrolü veritabanına baktığı için handler'da kalır.
/// </summary>
public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir kullanıcı seçilmelidir.");
    }
}
