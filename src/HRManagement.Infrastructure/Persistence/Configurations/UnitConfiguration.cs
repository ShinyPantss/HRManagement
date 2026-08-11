using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");

        builder.HasKey(u => u.Id).HasName("PK_Units");

        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();

        builder.ConfigureAuditColumns(u => u.CreatedAt, u => u.UpdatedAt);

        // Aynı departmanda aynı isimli birim iki kez olamaz.
        builder.HasIndex(u => new { u.DepartmentId, u.Name })
            .IsUnique()
            .HasDatabaseName("UQ_Units_Dept_Name");

        builder.HasIndex(u => u.DepartmentId).HasDatabaseName("IX_Units_DepartmentId");

        // Navigation property YOK: entity'ler saf POCO kaldığı için ilişki
        // yalnızca FK sütunu üzerinden kurulur. HasOne<T>() tip parametresi
        // hedefi söyler, WithMany() karşı tarafta koleksiyon olmadığını.
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .HasConstraintName("FK_Units_Departments")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
