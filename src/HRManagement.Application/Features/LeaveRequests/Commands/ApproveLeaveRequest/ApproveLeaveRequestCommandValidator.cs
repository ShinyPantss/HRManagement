using FluentValidation;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>
/// Input validation. Yalnızca "gelen Id kendi içinde anlamlı mı" sorulur.
/// "Talep var mı" ve "talep bekleyen durumda mı" kontrolleri veritabanına baktığı
/// için İŞ KURALIDIR ve handler'da kalır.
/// </summary>
public sealed class ApproveLeaveRequestCommandValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir izin talebi seçilmelidir.");
    }
}
