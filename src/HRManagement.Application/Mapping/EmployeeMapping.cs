using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Birden fazla handler aynı dönüşümü
/// kullandığı için buraya alındı; alan eklendiğinde tek dosya değişir.
/// </summary>
public static class EmployeeMapping
{
    /// <summary>
    /// <paramref name="canSeeNationalId"/> bilinçli olarak ZORUNLU: varsayılanı
    /// olsaydı yeni bir çağıran kararı atlayıp T.C. Kimlik'i sızdırabilirdi.
    /// Değeri EmployeeFieldVisibility.CanSeeNationalId ile hesaplanır.
    /// </summary>
    public static EmployeeDto ToDto(Employee employee, bool canSeeNationalId) => new()
    {
        Id = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        NationalId = canSeeNationalId ? employee.NationalId : null,
        Email = employee.Email,
        Phone = employee.Phone,
        BirthDate = employee.DateOfBirth,
        HireDate = employee.HireDate,
        Gender = (int?)employee.Gender,
        DepartmentId = employee.DepartmentId,
        UnitId = employee.UnitId,
        UserId = employee.UserId,
        ManagerId = employee.ManagerId,
        Seniority = (int?)employee.Seniority,
        AnnualLeaveDays = employee.AnnualLeaveDays,
        IsActive = employee.IsActive
    };
}
