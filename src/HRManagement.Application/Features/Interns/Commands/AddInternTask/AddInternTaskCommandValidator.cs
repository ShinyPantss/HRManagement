using FluentValidation;

namespace HRManagement.Application.Features.Interns.Commands.AddInternTask;

public sealed class AddInternTaskCommandValidator : AbstractValidator<AddInternTaskCommand>
{
    public AddInternTaskCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("Görev başlığı zorunludur.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");   // DB: nvarchar(200)

        RuleFor(c => c.Description)
            .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir."); // DB: nvarchar(1000)
    }
}
