using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>Entity → DTO dönüşümü tek yerde (alan eklendiğinde tek dosya değişir).</summary>
public static class UnitMapping
{
    public static UnitDto ToDto(Unit unit) => new()
    {
        Id = unit.Id,
        DepartmentId = unit.DepartmentId,
        Name = unit.Name
    };
}
