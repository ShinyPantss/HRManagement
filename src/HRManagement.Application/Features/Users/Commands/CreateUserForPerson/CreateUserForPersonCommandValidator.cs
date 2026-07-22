using FluentValidation;

namespace HRManagement.Application.Features.Users.Commands.CreateUserForPerson;

/// <summary>
/// Input validation. Kişinin varlığı ve "zaten hesabı var mı" kontrolü DB'ye
/// baktığı için İŞ KURALIDIR ve handler'da kalır.
/// </summary>
public sealed class CreateUserForPersonCommandValidator : AbstractValidator<CreateUserForPersonCommand>
{
    public CreateUserForPersonCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        RuleFor(command => command.Role)
            .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");

        // Hesap ya bir çalışana ya bir stajyere açılır — TAM OLARAK biri.
        RuleFor(command => command)
            .Must(command => (command.EmployeeId.HasValue) ^ (command.InternId.HasValue))
            .WithMessage("Hesap tam olarak bir çalışana veya bir stajyere açılmalıdır.");
    }
}
