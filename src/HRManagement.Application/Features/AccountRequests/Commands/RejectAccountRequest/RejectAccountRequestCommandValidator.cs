using FluentValidation;

namespace HRManagement.Application.Features.AccountRequests.Commands.RejectAccountRequest;

public sealed class RejectAccountRequestCommandValidator : AbstractValidator<RejectAccountRequestCommand>
{
    public RejectAccountRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir talep seçilmelidir.");

        RuleFor(command => command.ApproverUserId)
            .GreaterThan(0).WithMessage("Reddeden belirlenemedi.");

        RuleFor(command => command.Reason)
            .MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
    }
}
