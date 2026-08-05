using FluentValidation;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir çalışan seçilmelidir.");

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

        // Cinsiyet zorunlu (kullanıcı kararı, 2026-07-27); dolduysa geçerli enum olmalı.
        RuleFor(command => command.Gender)
            .NotNull().WithMessage("Cinsiyet seçimi zorunludur.");
        RuleFor(command => command.Gender!.Value)
            .IsInEnum().When(command => command.Gender.HasValue)
            .WithMessage("Geçerli bir cinsiyet seçilmelidir.");

        RuleFor(command => command.DepartmentId)
            .GreaterThan(0).WithMessage("Departman seçilmelidir.");

        RuleFor(command => command.HireDate)
            .GreaterThanOrEqualTo(command => command.BirthDate)
            .WithMessage("İşe giriş tarihi doğum tarihinden önce olamaz.");

        // T.C. Kimlik ZORUNLU DEĞİL; dolduysa 11 hane RAKAM olmalı (Create ile aynı).
        RuleFor(command => command.NationalId)
            .Length(11).WithMessage("T.C. Kimlik No 11 haneli olmalıdır.")
            .Matches("^[0-9]{11}$").WithMessage("T.C. Kimlik No yalnızca rakamlardan oluşmalıdır.")
            .When(command => !string.IsNullOrWhiteSpace(command.NationalId));

        // Opsiyonel alanlar: boş bırakılabilir, ama DOLU geldiyse anlamlı olmalı.
        RuleFor(command => command.UserId)
            .GreaterThan(0).When(command => command.UserId.HasValue)
            .WithMessage("Geçerli bir kullanıcı hesabı seçilmelidir.");

        RuleFor(command => command.ManagerId)
            .GreaterThan(0).When(command => command.ManagerId.HasValue)
            .WithMessage("Geçerli bir yönetici seçilmelidir.");

        RuleFor(command => command.UnitId)
            .GreaterThan(0).When(command => command.UnitId.HasValue)
            .WithMessage("Geçerli bir birim seçilmelidir.");

        RuleFor(command => command.AnnualLeaveDays)
            .GreaterThanOrEqualTo(0).When(command => command.AnnualLeaveDays.HasValue)
            .WithMessage("Yıllık izin günü negatif olamaz.");
    }
}
