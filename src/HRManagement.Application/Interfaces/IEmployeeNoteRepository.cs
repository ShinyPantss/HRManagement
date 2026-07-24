using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IEmployeeNoteRepository
{
    /// <summary>Çalışanın notları, yeniden eskiye sıralı (detay sayfası).</summary>
    Task<IEnumerable<EmployeeNote>> GetByEmployeeIdAsync(int employeeId);

    Task<int> AddAsync(EmployeeNote note);
}
