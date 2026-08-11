using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class InternTaskRepository : IInternTaskRepository
{
    private readonly HRManagementDbContext _context;

    public InternTaskRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<InternTask?> GetByIdAsync(int id)
    {
        return await _context.InternTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<InternTask>> GetByInternIdAsync(int internId)
    {
        return await _context.InternTasks
            .AsNoTracking()
            .Where(t => t.InternId == internId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> AddAsync(InternTask task)
    {
        // Status'un DB default'u (1) modele KONMADI; entity varsayılanı
        // (InternTaskStatus.Pending) her INSERT'te açıkça yazılır. Böylece iki
        // varsayılan birbirinden habersiz kayamaz — eski koddaki not da bunu diyordu.
        _context.InternTasks.Add(task);
        await _context.SaveChangesAsync();

        return task.Id;
    }

    public async Task UpdateAsync(InternTask task)
    {
        var mevcut = await _context.InternTasks.FirstOrDefaultAsync(t => t.Id == task.Id);

        if (mevcut is null)
            return;

        // InternId ve CreatedByUserId bilinçli olarak DIŞARIDA: bir görev
        // açıldıktan sonra sahibi de atayanı da değişmez. Eski elle yazılan
        // UPDATE cümlesi de tam olarak bu dört sütunu yazıyordu.
        mevcut.Title = task.Title;
        mevcut.Description = task.Description;
        mevcut.Status = task.Status;
        mevcut.DueDate = task.DueDate;

        await _context.SaveChangesAsync();
    }
}
