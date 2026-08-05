using FluentValidation.TestHelper;
using HRManagement.Application.Features.Employees.Commands.CreateEmployee;
using HRManagement.Application.Features.Employees.Commands.UpdateEmployee;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Validators;

/// <summary>
/// T.C. Kimlik No format kuralı. Alan ZORUNLU DEĞİLDİR (mevcut davranış korunur:
/// kayıt T.C.'siz açılabiliyordu), ama DOLU geldiyse 11 hane rakam olmalıdır.
/// Kural Create ve Update'te birebir aynıdır — biri gevşek kalırsa kayıt açılışta
/// denetlenip güncellemede bozulabilirdi.
///
/// Benzersizlik kuralı burada DEĞİL: veritabanına baktığı için handler'da
/// (GetByNationalIdAsync), asıl garantisi de db/20_employee_nationalid_unique.sql.
/// </summary>
public class EmployeeNationalIdValidationTests
{
    private readonly CreateEmployeeCommandValidator _createValidator = new();
    private readonly UpdateEmployeeCommandValidator _updateValidator = new();

    private static CreateEmployeeCommand ValidCreateCommand() => new(
        FirstName: "Mücahit",
        LastName: "Can",
        NationalId: "12345678901",
        Email: "mucahit@example.com",
        Phone: null,
        BirthDate: new DateTime(2000, 1, 1),
        HireDate: new DateTime(2024, 1, 1),
        Gender: Gender.Male,
        DepartmentId: 1,
        UnitId: null,
        UserId: null,
        ManagerId: null,
        Seniority: SeniorityLevel.Uzman,
        AnnualLeaveDays: null,
        CreatedByUserId: 1,
        RequestLoginAccount: false);

    private static UpdateEmployeeCommand ValidUpdateCommand() => new(
        Id: 1,
        FirstName: "Mücahit",
        LastName: "Can",
        NationalId: "12345678901",
        Email: "mucahit@example.com",
        Phone: null,
        BirthDate: new DateTime(2000, 1, 1),
        HireDate: new DateTime(2024, 1, 1),
        Gender: Gender.Male,
        DepartmentId: 1,
        UnitId: null,
        UserId: null,
        ManagerId: null,
        Seniority: SeniorityLevel.Uzman,
        AnnualLeaveDays: null,
        IsActive: true);

    [Theory]
    [InlineData("1234567890")]      // 10 hane
    [InlineData("123456789012")]    // 12 hane
    [InlineData("1234567890A")]     // harf içeriyor
    [InlineData("12345 678901")]    // boşluk içeriyor
    public void Gecersiz_TC_hem_ekleme_hem_guncellemede_reddedilir(string nationalId)
    {
        _createValidator.TestValidate(ValidCreateCommand() with { NationalId = nationalId })
            .ShouldHaveValidationErrorFor(c => c.NationalId);

        _updateValidator.TestValidate(ValidUpdateCommand() with { NationalId = nationalId })
            .ShouldHaveValidationErrorFor(c => c.NationalId);
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData(null)]              // alan zorunlu değil
    [InlineData("")]
    [InlineData("   ")]
    public void Gecerli_veya_bos_TC_kabul_edilir(string? nationalId)
    {
        _createValidator.TestValidate(ValidCreateCommand() with { NationalId = nationalId })
            .ShouldNotHaveValidationErrorFor(c => c.NationalId);

        _updateValidator.TestValidate(ValidUpdateCommand() with { NationalId = nationalId })
            .ShouldNotHaveValidationErrorFor(c => c.NationalId);
    }
}
