using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Birden fazla handler aynı dönüşümü
/// kullandığı için buraya alındı; alan eklendiğinde tek dosya değişir.
/// </summary>
public static class DepartmentMapping
{
    public static DepartmentDto ToDto(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description
    };
}
