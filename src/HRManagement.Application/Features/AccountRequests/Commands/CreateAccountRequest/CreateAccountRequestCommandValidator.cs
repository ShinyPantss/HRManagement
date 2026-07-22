using FluentValidation;

namespace HRManagement.Application.Features.AccountRequests.Commands.CreateAccountRequest;

public sealed class CreateAccountRequestCommandValidator : AbstractValidator<CreateAccountRequestCommand>
{
    public CreateAccountRequestCommandValidator()
    {
        RuleFor(command => command.RequestedByUserId)
            .GreaterThan(0).WithMessage("Talep sahibi belirlenemedi.");

        RuleFor(command => command.SuggestedRole)
            .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");

        RuleFor(command => command.Note)
            .MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");

        // Talep tam olarak bir çalışana VEYA bir stajyere ait olmalı.
        RuleFor(command => command)
            .Must(command => command.EmployeeId.HasValue ^ command.InternId.HasValue)
            .WithMessage("Talep tam olarak bir çalışan veya bir stajyer için açılmalıdır.");
    }
}
