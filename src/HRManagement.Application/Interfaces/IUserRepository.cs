using HRManagement.Domain.Entities;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);

    /// <summary>
    /// Hesabı oluşturur ve verilen kişiye (çalışan VEYA stajyer) TEK TRANSACTION'da
    /// bağlar. İki yazma birlikte başarılı olur ya da hiç olmaz — yarısı patlarsa
    /// sahipsiz hesap kalmaz. Yeni User'ın Id'sini döndürür.
    /// </summary>
    Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId);
}