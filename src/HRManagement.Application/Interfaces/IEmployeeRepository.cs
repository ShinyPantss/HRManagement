using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<int> AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int id);

    // Silme öncesi bağımlılık kontrolleri. Tüm kayıtları çekip bellekte saymak
    // yerine "var mı?" sorusunu veritabanına sorarız: ilk eşleşmede durur.
    Task<bool> ExistsByDepartmentIdAsync(int departmentId);
    Task<bool> ExistsByUserIdAsync(int userId);
    Task<bool> ExistsByManagerIdAsync(int managerId);

    /// <summary>
    /// User ↔ Employee köprüsü: giriş yapan hesabın çalışan kaydını bulur.
    /// Yetki kararları bu metotla DB'den çözülür (JWT claim'i bayatlayabilir).
    /// </summary>
    Task<Employee?> GetByUserIdAsync(int userId);

    /// <summary>E-posta benzersizliği iş kuralı için (DB'deki UNIQUE kısıtın ön kontrolü).</summary>
    Task<Employee?> GetByEmailAsync(string email);

    /// <summary>
    /// "managerEmployeeId, subordinateEmployeeId'nin yönetici zincirinde YUKARIDA mı?"
    /// İzin onayı yetkisi ve döngü önleme (çalışan kendi astına bağlanamaz) bunu kullanır.
    /// </summary>
    Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId);
}