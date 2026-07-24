using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Features.Interns.Shared;

/// <summary>
/// Mentorluk işlemlerinin ortak yetki kuralı: bir stajyer üzerinde görev/not
/// işlemi yapabilmek için istekçinin ÇALIŞAN KAYDI o stajyerin MentorId'si
/// olmalıdır. Rol önemsizdir (Manager da Employee da mentor olabilir) —
/// yetki rolden değil İLİŞKİDEN doğar, EmployeeVisibility ile aynı felsefe.
/// </summary>
public sealed class MentorshipGuard
{
    private readonly IInternRepository _internRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public MentorshipGuard(IInternRepository internRepository, IEmployeeRepository employeeRepository)
    {
        _internRepository = internRepository;
        _employeeRepository = employeeRepository;
    }

    /// <summary>
    /// Stajyeri yükler ve istekçinin onun mentoru olduğunu doğrular.
    /// Değilse ValidationException — stajyerin varlığı da sızdırılmaz
    /// (iki durumda da aynı mesaj verilmez ama kayıt içeriği dönmez).
    /// </summary>
    public async Task<Intern> EnsureMentorAsync(int internId, int requesterUserId)
    {
        var intern = await _internRepository.GetByIdAsync(internId);

        if (intern is null)
            throw new ValidationException("Stajyer bulunamadı.");

        var mentorEmployee = await _employeeRepository.GetByUserIdAsync(requesterUserId);

        if (mentorEmployee is null || intern.MentorId != mentorEmployee.Id)
            throw new ValidationException("Bu stajyerin mentoru değilsiniz.");

        return intern;
    }
}
