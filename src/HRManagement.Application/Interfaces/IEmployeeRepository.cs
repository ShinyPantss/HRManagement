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
}