using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Interns.Shared;

/// <summary>
/// Mentorluk işlemlerinin ortak yetki kuralı.
///   YAZMA (görev/not): istekçinin ÇALIŞAN KAYDI stajyerin MentorId'si olmalı —
///     rol önemsiz, yetki İLİŞKİDEN doğar (EnsureMentorAsync).
///   GÖRÜNTÜLEME (detay): mentor VEYA HR/Admin — HR/Admin salt-okur gözlemci
///     (görev/not EKLEYEMEZ; o kural yazma tarafında kalır) (EnsureCanViewAsync).
/// </summary>
public sealed class MentorshipGuard
{
    private readonly IInternRepository _internRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public MentorshipGuard(
        IInternRepository internRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _internRepository = internRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Stajyeri yükler ve istekçinin onun MENTORU olduğunu doğrular (yazma yetkisi).
    /// Değilse ValidationException.
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

    /// <summary>
    /// Detay GÖRÜNTÜLEME yetkisi: mentor VEYA HR/Admin. HR/Admin yalnızca gözlemler
    /// (görev/not ekleyemez — o kural EnsureMentorAsync'te kalır).
    /// </summary>
    public async Task<Intern> EnsureCanViewAsync(int internId, int requesterUserId)
    {
        var intern = await _internRepository.GetByIdAsync(internId);

        if (intern is null)
            throw new ValidationException("Stajyer bulunamadı.");

        // Mentor mu? (ilişki — yazma yetkisiyle aynı test)
        var employee = await _employeeRepository.GetByUserIdAsync(requesterUserId);
        if (employee is not null && intern.MentorId == employee.Id)
            return intern;

        // Değilse: HR/Admin salt-okur gözlemci olabilir.
        var user = await _userRepository.GetByIdAsync(requesterUserId);
        if (user is not null && user.Role is Role.HR or Role.Admin)
            return intern;

        throw new ValidationException("Bu stajyerin detayını görme yetkiniz yok.");
    }
}
