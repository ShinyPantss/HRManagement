using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IInternRepository
{
    Task<Intern?> GetByIdAsync(int id);
    Task<IEnumerable<Intern>> GetAllAsync();
    Task<int> AddAsync(Intern intern);
    Task UpdateAsync(Intern intern);
    Task DeleteAsync(int id);

    /// <summary>
    /// Stajyeri, izin taleplerini ve hesap taleplerini TEK TRANSACTION'da siler;
    /// varsa login hesabını pasife alır (hard-delete değil — audit referansları).
    /// Stajyer pasife alınamadığı için (IsActive yok) izin geçmişi de cascade edilir.
    /// </summary>
    Task DeleteWithAccountAsync(int internId, int? userId);

    // Silme öncesi bağımlılık kontrolleri.
    Task<bool> ExistsByDepartmentIdAsync(int departmentId);
    Task<bool> ExistsByMentorIdAsync(int mentorId);
    Task<bool> ExistsByUserIdAsync(int userId);

    /// <summary>User ↔ Intern köprüsü: giriş yapan hesabın stajyer kaydını bulur.</summary>
    Task<Intern?> GetByUserIdAsync(int userId);
}