using FluentValidation;

namespace HRManagement.Application.Features.Interns.Commands.AddInternNote;

public sealed class AddInternNoteCommandValidator : AbstractValidator<AddInternNoteCommand>
{
    public AddInternNoteCommandValidator()
    {
        RuleFor(c => c.Content)
            .NotEmpty().WithMessage("Not içeriği boş olamaz.")
            .MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir."); // DB: nvarchar(1000)
    }
}
