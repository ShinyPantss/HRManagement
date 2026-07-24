using FluentValidation.TestHelper;
using HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Validators;

/// <summary>
/// Gereksinim dokümanının örnek test senaryosu (§7):
/// "İzin başlangıç tarihi > bitiş tarihi olduğunda hata dönmesi."
/// Validator saf girdi kontrolü olduğu için DB'siz test edilir;
/// FluentValidation'ın TestHelper'ı hangi kuralın patladığını da doğrular.
/// </summary>
public class CreateLeaveRequestCommandValidatorTests
{
    private readonly CreateLeaveRequestCommandValidator _validator = new();

    private static CreateLeaveRequestCommand ValidCommand() => new(
        RequesterUserId: 1,
        Type: LeaveType.Annual,
        StartDate: new DateTime(2026, 8, 3),
        EndDate: new DateTime(2026, 8, 7),
        Description: "Yaz tatili",
        MedicalReport: null);

    [Fact]
    public void baslangic_bitisten_sonra_ise_hata_doner()
    {
        var command = ValidCommand() with
        {
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 3)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public void gecerli_talep_hatasiz_gecer()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void tek_gunluk_izin_gecerlidir()
    {
        // Başlangıç == bitiş meşru bir taleptir (1 günlük izin); kural
        // GreaterThanOrEqualTo olduğu için geçmeli.
        var command = ValidCommand() with
        {
            StartDate = new DateTime(2026, 8, 3),
            EndDate = new DateTime(2026, 8, 3)
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public void tanimsiz_izin_turu_reddedilir()
    {
        // C# enum'ları aralık kontrolü yapmaz: (LeaveType)99 derlenir ve çalışır.
        // Bu yüzden IsInEnum kuralı şarttır — bu test o kuralın sigortasıdır.
        var command = ValidCommand() with { Type = (LeaveType)99 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Type);
    }

    [Fact]
    public void talep_sahibi_belirlenemiyorsa_hata_doner()
    {
        var command = ValidCommand() with { RequesterUserId = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RequesterUserId);
    }
}
