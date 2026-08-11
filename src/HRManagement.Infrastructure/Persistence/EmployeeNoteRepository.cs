using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class EmployeeNoteRepository : IEmployeeNoteRepository
{
    private readonly HRManagementDbContext _context;

    public EmployeeNoteRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeNote>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.EmployeeNotes
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> AddAsync(EmployeeNote note)
    {
        _context.EmployeeNotes.Add(note);
        await _context.SaveChangesAsync();

        return note.Id;
    }
}
