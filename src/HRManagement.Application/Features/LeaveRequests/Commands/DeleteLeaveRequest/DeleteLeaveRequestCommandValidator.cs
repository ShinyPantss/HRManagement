using FluentValidation;

namespace HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;

/// <summary>
/// Input validation. "Talep var mı" kontrolü veritabanına baktığı için
/// İŞ KURALIDIR ve handler'da kalır.
/// </summary>
public sealed class DeleteLeaveRequestCommandValidator : AbstractValidator<DeleteLeaveRequestCommand>
{
    public DeleteLeaveRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir izin talebi seçilmelidir.");
    }
}
