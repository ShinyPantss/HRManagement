using FluentValidation;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>
/// Input validation. "Talep var mı" ve "talep bekleyen durumda mı" kontrolleri
/// veritabanına baktığı için İŞ KURALIDIR ve handler'da kalır.
/// </summary>
public sealed class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0).WithMessage("Geçerli bir izin talebi seçilmelidir.");

        // Reason opsiyoneldir (boş bırakılabilir); sadece uzunluğu sınırlanır.
        RuleFor(command => command.Reason)
            .MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
    }
}
