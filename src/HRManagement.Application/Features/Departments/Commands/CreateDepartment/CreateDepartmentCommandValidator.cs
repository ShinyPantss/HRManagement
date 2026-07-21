using FluentValidation;

namespace HRManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// Burada yalnızca "gelen veri kendi içinde geçerli mi" sorulur; veritabanına bakan
/// iş kuralları (ör. "bu departman zaten var mı") handler'da kalır.
/// </summary>
public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(100).WithMessage("Departman adı en fazla 100 karakter olabilir.");

        RuleFor(command => command.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
