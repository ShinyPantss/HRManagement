using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class InternNoteRepository : IInternNoteRepository
{
    private readonly HRManagementDbContext _context;

    public InternNoteRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InternNote>> GetByInternIdAsync(int internId)
    {
        return await _context.InternNotes
            .AsNoTracking()
            .Where(n => n.InternId == internId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> AddAsync(InternNote note)
    {
        _context.InternNotes.Add(note);
        await _context.SaveChangesAsync();

        return note.Id;
    }
}
