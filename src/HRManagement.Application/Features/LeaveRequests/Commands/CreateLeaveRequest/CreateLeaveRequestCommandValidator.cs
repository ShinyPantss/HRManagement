using FluentValidation;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
/// Burada yalnızca "gelen veri kendi içinde geçerli mi" sorulur; veritabanına bakan
/// iş kuralları (ör. "çalışanın izin hakkı yeterli mi", "tarihler başka bir izinle
/// çakışıyor mu") handler'da kalır.
/// </summary>
public sealed class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        // Claim'den gelir; 0/negatifse token çözümünde bir şeyler ters gitmiştir.
        RuleFor(command => command.RequesterUserId)
            .GreaterThan(0).WithMessage("Talep sahibi belirlenemedi.");

        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("Geçerli bir izin türü seçilmelidir.");

        // Tarih sırası kontrolü: iki alan da istekte geldiği için DB'ye bakmaya
        // gerek yok — bu bir input validation'dır, iş kuralı değil.
        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        RuleFor(command => command.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");

        // Hastalık izninde rapor zorunlu (girdi kuralı — DB'ye bakmadan bilinir).
        RuleFor(command => command.MedicalReport)
            .NotEmpty().When(command => command.Type == LeaveType.Sick)
            .WithMessage("Hastalık izni için rapor bilgisi zorunludur.");

        RuleFor(command => command.MedicalReport)
            .MaximumLength(500).WithMessage("Rapor bilgisi en fazla 500 karakter olabilir.");
    }
}
