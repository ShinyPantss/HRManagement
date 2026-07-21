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

        RuleFor(command => command.Position)
            .MaximumLength(100).WithMessage("Pozisyon en fazla 100 karakter olabilir.");

        RuleFor(command => command.DepartmentId)
            .GreaterThan(0).WithMessage("Departman seçilmelidir.");

        RuleFor(command => command.HireDate)
            .GreaterThanOrEqualTo(command => command.BirthDate)
            .WithMessage("İşe giriş tarihi doğum tarihinden önce olamaz.");
    }
}
