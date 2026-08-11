using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace HRManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core bağlamı. Şema eşlemesi ATTRIBUTE ile değil, Fluent API ile
/// Configurations/ altında yapılır.
///
/// Neden Fluent: attribute kullanmak Domain entity'lerine
/// Microsoft.EntityFrameworkCore referansı sokardı. Domain'in hiçbir şeye
/// referans vermemesi projenin temel kuralı (bkz. CLAUDE.md) — veri erişimi
/// bir DETAYDIR, iş katmanı onu bilmemelidir. Fluent API sayesinde entity'ler
/// saf POCO kalıyor ve bu geçişte Domain'e tek satır dokunulmadı.
/// </summary>
public class HRManagementDbContext : DbContext
{
    public HRManagementDbContext(DbContextOptions<HRManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Intern> Interns => Set<Intern>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<EmployeeNote> EmployeeNotes => Set<EmployeeNote>();
    public DbSet<InternTask> InternTasks => Set<InternTask>();
    public DbSet<AccountRequest> AccountRequests => Set<AccountRequest>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // EF varsayılan olarak HER foreign key sütununa index üretir. Mevcut
        // şemamızda (db/05_full_setup.sql, 4. bölüm) index'ler BİLİNÇLİ seçilmiş:
        // yalnızca gerçekten filtrelenen sütunlarda var. Bu konvansiyon açık
        // kalsaydı baseline migration, veritabanında OLMAYAN yarım düzine index
        // eklemek isterdi ve model ile gerçek şema ayrışırdı.
        //
        // Model önce mevcut şemaya SADIK kurulur. Eksik index'ler gerçekten
        // gerekiyorsa bu ayrı bir karardır ve kendi migration'ıyla eklenir.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurations/ altındaki IEntityTypeConfiguration<T> sınıflarının
        // hepsini bulur. Yeni entity eklendiğinde burayı düzenlemek gerekmez —
        // unutulup sessizce konvansiyonlara düşme riski ortadan kalkar.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HRManagementDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
