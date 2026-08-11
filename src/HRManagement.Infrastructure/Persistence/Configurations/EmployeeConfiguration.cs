using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", table =>
        {
            table.HasCheckConstraint(
                "CK_Employees_AnnualLeaveDays",
                "AnnualLeaveDays IS NULL OR AnnualLeaveDays >= 0");

            table.HasCheckConstraint(
                "CK_Employees_Seniority",
                "Seniority IS NULL OR Seniority BETWEEN 1 AND 6");

            table.HasCheckConstraint(
                "CK_Employees_Gender",
                "Gender IS NULL OR Gender BETWEEN 1 AND 2");

            table.HasCheckConstraint(
                "CK_Employees_NationalId",
                "NationalId IS NULL OR NationalId LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
        });

        builder.HasKey(e => e.Id).HasName("PK_Employees");

        builder.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NationalId).HasMaxLength(11);
        builder.Property(e => e.Email).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);

        // Saat bileşeni taşımayan alanlar date olarak saklanır: doğum günü ve
        // işe giriş tarihinde saat yoktur, datetime2 tutmak yanlış karşılaştırma
        // (ör. "bugün işe girenler") riski yaratırdı.
        builder.Property(e => e.DateOfBirth).HasColumnType(AuditColumnConfiguration.DateType);
        builder.Property(e => e.HireDate).HasColumnType(AuditColumnConfiguration.DateType);

        // ── IsActive'in DB default'u (DF_Employees_IsActive = 1) MODELLENMEDİ ──
        // EF'te HasDefaultValue(...) kullanmak property'yi "ValueGeneratedOnAdd"
        // yapar; EF de INSERT'te değeri CLR varsayılanına eşitse SÜTUNU HİÇ GÖNDERMEZ.
        // bool'un CLR varsayılanı false olduğu için IsActive=false yazmak isteyen
        // bir kayıt sütunsuz gider ve veritabanı default'u onu SESSİZCE true yapar.
        // Pasife alma bu projede gerçek bir iş kuralı (silinen çalışanın hesabı),
        // dolayısıyla değeri her zaman açıkça yazıyoruz. Default DB'de durmaya
        // devam ediyor — uygulama dışı INSERT'ler için hâlâ değerli.
        // Aynı gerekçe Users.IsActive, LeaveRequests.Status/WorkingDays,
        // InternTasks.Status, AccountRequests.Status için de geçerlidir.

        builder.ConfigureAuditColumns(e => e.CreatedAt, e => e.UpdatedAt);

        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Employees_Email");

        // FILTRELİ unique index — UNIQUE constraint DEĞİL. SQL Server'da UNIQUE
        // constraint NULL'ları birbirinin aynısı sayar; ikinci "T.C.'si girilmemiş"
        // çalışan eklenemezdi. Filtre NULL satırları index dışında bırakır.
        builder.HasIndex(e => e.NationalId)
            .IsUnique()
            .HasFilter("[NationalId] IS NOT NULL")
            .HasDatabaseName("UX_Employees_NationalId");

        builder.HasIndex(e => e.DepartmentId).HasDatabaseName("IX_Employees_DepartmentId");
        builder.HasIndex(e => e.UnitId).HasDatabaseName("IX_Employees_UnitId");
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_Employees_UserId");
        builder.HasIndex(e => e.ManagerId).HasDatabaseName("IX_Employees_ManagerId");

        // Tüm FK'larda NoAction: veritabanındaki karşılıkları da öyle. EF zorunlu
        // ilişkilerde VARSAYILAN olarak Cascade uygular — yazılmasaydı model,
        // "departman silinince çalışanları da sil" derdi. Silme kuralları bu
        // projede Application katmanında, bilinçli olarak ele alınıyor.
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .HasConstraintName("FK_Employees_Departments")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(e => e.UnitId)
            .HasConstraintName("FK_Employees_Units")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("FK_Employees_Users")
            .OnDelete(DeleteBehavior.NoAction);

        // Kendine referans: yöneticinin kendisi de bir çalışandır.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .HasConstraintName("FK_Employees_Manager")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
