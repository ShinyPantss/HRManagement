using FluentValidation;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// Burada yalnızca "gelen veri kendi içinde geçerli mi" sorulur; veritabanına bakan
/// iş kuralları (ör. "bu e-posta zaten kayıtlı mı") handler'da kalır.
/// </summary>
public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
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

        RuleFor(command => command.Seniority!.Value)
            .IsInEnum().When(command => command.Seniority.HasValue)
            .WithMessage("Geçerli bir kıdem seçilmelidir.");

        RuleFor(command => command.DepartmentId)
            .GreaterThan(0).WithMessage("Departman seçilmelidir.");

        // İki alanı birbirine göre karşılaştıran kural da input validation'dır:
        // karar için veritabanına bakmaya gerek yok, mesajın kendi içinde tutarlılığı.
        RuleFor(command => command.HireDate)
            .GreaterThanOrEqualTo(command => command.BirthDate)
            .WithMessage("İşe giriş tarihi doğum tarihinden önce olamaz.");

        // Opsiyonel alanlar: boş bırakılabilir, ama DOLU geldiyse anlamlı olmalı.
        RuleFor(command => command.UserId)
            .GreaterThan(0).When(command => command.UserId.HasValue)
            .WithMessage("Geçerli bir kullanıcı hesabı seçilmelidir.");

        RuleFor(command => command.ManagerId)
            .GreaterThan(0).When(command => command.ManagerId.HasValue)
            .WithMessage("Geçerli bir yönetici seçilmelidir.");

        RuleFor(command => command.AnnualLeaveDays)
            .GreaterThanOrEqualTo(0).When(command => command.AnnualLeaveDays.HasValue)
            .WithMessage("Yıllık izin günü negatif olamaz.");
    }
}
