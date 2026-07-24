using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IInternTaskRepository
{
    Task<InternTask?> GetByIdAsync(int id);

    /// <summary>Stajyerin görevleri, yeniden eskiye sıralı.</summary>
    Task<IEnumerable<InternTask>> GetByInternIdAsync(int internId);

    Task<int> AddAsync(InternTask task);
    Task UpdateAsync(InternTask task);
}
