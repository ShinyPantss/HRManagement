using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IInternNoteRepository
{
    /// <summary>Stajyerin mentor notları, yeniden eskiye sıralı.</summary>
    Task<IEnumerable<InternNote>> GetByInternIdAsync(int internId);

    Task<int> AddAsync(InternNote note);
}
