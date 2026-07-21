using FluentValidation;

namespace HRManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir departman seçilmelidir.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(100).WithMessage("Departman adı en fazla 100 karakter olabilir.");

        RuleFor(command => command.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
