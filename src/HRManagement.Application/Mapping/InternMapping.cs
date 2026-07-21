using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Birden fazla handler aynı dönüşümü
/// kullandığı için buraya alındı; alan eklendiğinde tek dosya değişir.
/// </summary>
public static class InternMapping
{
    public static InternDto ToDto(Intern intern) => new()
    {
        Id = intern.Id,
        FirstName = intern.FirstName,
        LastName = intern.LastName,
        Email = intern.Email,
        University = intern.University,
        Major = intern.Major,
        Grade = intern.Grade,
        StartDate = intern.StartDate,
        EndDate = intern.EndDate,
        MentorId = intern.MentorId,
        DepartmentId = intern.DepartmentId
    };
}
