using FluentValidation;

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// "Kullanıcı bulunamadı." ve e-posta benzersizliği veritabanına bakan iş kurallarıdır,
/// onlar handler'da kalır.
/// </summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir kullanıcı seçilmelidir.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(command => command.Role)
            .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");
    }
}
