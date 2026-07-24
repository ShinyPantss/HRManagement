using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IUnitRepository
{
    Task<IEnumerable<Unit>> GetAllAsync();
    Task<Unit?> GetByIdAsync(int id);
}
