using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class UnitRepository : IUnitRepository
{
    private readonly HRManagementDbContext _context;

    public UnitRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    // AsNoTracking: bu sorgular yalnızca OKUMA içindir. Varsayılan davranışta EF
    // dönen her entity'nin bir kopyasını saklar ki sonradan neyin değiştiğini
    // anlayabilsin; hiç güncellenmeyecek listelerde bu boşa bellek ve CPU'dur.
    // Ayrıca eski Dapper davranışıyla aynı: dönen nesneler bağlamdan kopuk.
    public async Task<IEnumerable<Unit>> GetAllAsync()
    {
        return await _context.Units
            .AsNoTracking()
            .OrderBy(u => u.DepartmentId)
            .ThenBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<Unit?> GetByIdAsync(int id)
    {
        return await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
