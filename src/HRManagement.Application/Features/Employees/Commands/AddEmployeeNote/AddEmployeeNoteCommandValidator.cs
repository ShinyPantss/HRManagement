using FluentValidation;

namespace HRManagement.Application.Features.Employees.Commands.AddEmployeeNote;

public sealed class AddEmployeeNoteCommandValidator : AbstractValidator<AddEmployeeNoteCommand>
{
    public AddEmployeeNoteCommandValidator()
    {
        RuleFor(c => c.Content)
            .NotEmpty().WithMessage("Not içeriği boş olamaz.")
            .MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir."); // DB: nvarchar(1000)
    }
}
