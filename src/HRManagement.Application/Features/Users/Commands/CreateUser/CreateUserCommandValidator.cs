using FluentValidation;

namespace HRManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// Burada yalnızca "gelen veri kendi içinde geçerli mi" sorulur; veritabanına bakan
/// iş kuralları (kullanıcı adı / e-posta benzersizliği) handler'da kalır.
/// </summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
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

        // IsInEnum: enum'a tanımsız bir sayı cast edilerek gönderilirse (ör. Role = 99)
        // yakalar. C# enum'ları aralık kontrolü yapmaz, bu yüzden şart.
        RuleFor(command => command.Role)
            .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");
    }
}
