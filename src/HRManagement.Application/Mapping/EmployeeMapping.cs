using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Birden fazla handler aynı dönüşümü
/// kullandığı için buraya alındı; alan eklendiğinde tek dosya değişir.
/// </summary>
public static class EmployeeMapping
{
    public static EmployeeDto ToDto(Employee employee) => new()
    {
        Id = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        NationalId = employee.NationalId,
        Email = employee.Email,
        Phone = employee.Phone,
        BirthDate = employee.DateOfBirth,
        HireDate = employee.HireDate,
        Position = employee.Position,
        DepartmentId = employee.DepartmentId,
        UserId = employee.UserId,
        ManagerId = employee.ManagerId,
        AnnualLeaveDays = employee.AnnualLeaveDays,
        IsActive = employee.IsActive
    };
}
