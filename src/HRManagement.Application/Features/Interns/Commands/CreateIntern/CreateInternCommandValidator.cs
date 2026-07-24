using FluentValidation;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// Burada yalnızca "gelen veri kendi içinde geçerli mi" sorulur; veritabanına bakan
/// iş kuralları (ör. "mentor gerçekten var mı") handler'da kalır.
/// </summary>
public sealed class CreateInternCommandValidator : AbstractValidator<CreateInternCommand>
{
    public CreateInternCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(command => command.University)
            .NotEmpty().WithMessage("Üniversite zorunludur.")
            .MaximumLength(200).WithMessage("Üniversite en fazla 200 karakter olabilir.");

        RuleFor(command => command.Major)
            .MaximumLength(100).WithMessage("Bölüm en fazla 100 karakter olabilir.");

        RuleFor(command => command.Grade)
            .InclusiveBetween(1, 8).WithMessage("Sınıf 1-8 arasında olmalıdır.");

        // Tarih sırası kuralı da input validation'dır: karar için veritabanına
        // bakmaya gerek yok, mesajın kendi içinde tutarlılığı.
        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("Staj başlangıç tarihi bitiş tarihinden sonra olamaz.");

        RuleFor(command => command.DepartmentId)
            .GreaterThan(0).WithMessage("Departman seçilmelidir.");

        RuleFor(command => command.UnitId)
            .GreaterThan(0).When(command => command.UnitId.HasValue)
            .WithMessage("Geçerli bir birim seçilmelidir.");

        // Otomatik hesap talebinin "talep eden"i; oturumdan (JWT) gelir, 0 olmamalı.
        RuleFor(command => command.CreatedByUserId)
            .GreaterThan(0).WithMessage("Kaydı açan oturum belirlenemedi.");
    }
}
