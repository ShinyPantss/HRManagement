using FluentValidation;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Interns.Commands.UpdateMyInternTaskStatus;

public sealed class UpdateMyInternTaskStatusCommandValidator
    : AbstractValidator<UpdateMyInternTaskStatusCommand>
{
    public UpdateMyInternTaskStatusCommandValidator()
    {
        RuleFor(c => c.NewStatus)
            .Must(status => Enum.IsDefined(typeof(InternTaskStatus), status))
            .WithMessage("Geçersiz görev durumu.");
    }
}
