using FluentValidation.TestHelper;
using HRManagement.Application.Features.Interns.Commands.CreateIntern;

namespace HRManagement.Application.Tests.Validators;

/// <summary>
/// Gereksinim dokümanının örnek test senaryosu (§7):
/// "Stajyer eklerken zorunlu alanlar dolu değilse validasyon hatası."
/// </summary>
public class CreateInternCommandValidatorTests
{
    private readonly CreateInternCommandValidator _validator = new();

    private static CreateInternCommand ValidCommand() => new(
        FirstName: "Mücahit",
        LastName: "Can",
        Email: "mucahit@example.com",
        University: "Örnek Üniversitesi",
        Major: "Bilgisayar Mühendisliği",
        Grade: 3,
        StartDate: new DateTime(2026, 6, 1),
        EndDate: new DateTime(2026, 9, 1),
        MentorId: null,
        DepartmentId: 1);

    [Fact]
    public void zorunlu_alanlar_bos_ise_her_biri_icin_hata_doner()
    {
        var command = ValidCommand() with
        {
            FirstName = "",
            LastName = "",
            Email = "",
            University = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FirstName);
        result.ShouldHaveValidationErrorFor(c => c.LastName);
        result.ShouldHaveValidationErrorFor(c => c.Email);
        result.ShouldHaveValidationErrorFor(c => c.University);
    }

    [Fact]
    public void gecerli_stajyer_hatasiz_gecer()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void staj_bitisi_baslangictan_once_olamaz()
    {
        var command = ValidCommand() with
        {
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 6, 1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void sinif_1_ile_8_disinda_olamaz(int grade)
    {
        var command = ValidCommand() with { Grade = grade };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Grade);
    }
}
