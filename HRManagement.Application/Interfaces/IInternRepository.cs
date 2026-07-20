using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IInternRepository
{
    Task<Intern?> GetByIdAsync(int id);
    Task<IEnumerable<Intern>> GetAllAsync();
    Task<int> AddAsync(Intern intern);
    Task UpdateAsync(Intern intern);
    Task DeleteAsync(int id);
}