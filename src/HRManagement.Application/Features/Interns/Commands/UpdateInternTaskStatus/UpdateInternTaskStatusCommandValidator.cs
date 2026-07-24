using FluentValidation;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Interns.Commands.UpdateInternTaskStatus;

public sealed class UpdateInternTaskStatusCommandValidator : AbstractValidator<UpdateInternTaskStatusCommand>
{
    public UpdateInternTaskStatusCommandValidator()
    {
        RuleFor(c => c.NewStatus)
            .Must(status => Enum.IsDefined(typeof(InternTaskStatus), status))
            .WithMessage("Geçersiz görev durumu.");
    }
}
