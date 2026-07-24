using FluentValidation;

namespace HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;

public sealed class ApproveAccountRequestCommandValidator : AbstractValidator<ApproveAccountRequestCommand>
{
    public ApproveAccountRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir talep seçilmelidir.");

        RuleFor(command => command.ApproverUserId)
            .GreaterThan(0).WithMessage("Onaylayan belirlenemedi.");

        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        // Rol artık onayda seçilmiyor (talepten, kişiden türetilmiş olarak gelir) →
        // doğrulanacak rol alanı yok.
    }
}
