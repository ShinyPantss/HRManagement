using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class InternConfiguration : IEntityTypeConfiguration<Intern>
{
    public void Configure(EntityTypeBuilder<Intern> builder)
    {
        builder.ToTable("Interns");

        builder.HasKey(i => i.Id).HasName("PK_Interns");

        builder.Property(i => i.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(i => i.LastName).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Email).HasMaxLength(100).IsRequired();
        builder.Property(i => i.University).HasMaxLength(150).IsRequired();
        builder.Property(i => i.Major).HasMaxLength(100).IsRequired();

        builder.Property(i => i.StartDate).HasColumnType(AuditColumnConfiguration.DateType);
        builder.Property(i => i.EndDate).HasColumnType(AuditColumnConfiguration.DateType);

        builder.ConfigureAuditColumns(i => i.CreatedAt, i => i.UpdatedAt);

        // Employees.NationalId'nin aksine burada FİLTRESİZ unique: Interns.Email
        // NOT NULL olduğu için "birden çok NULL" sorunu hiç doğmuyor.
        builder.HasIndex(i => i.Email).IsUnique().HasDatabaseName("UQ_Interns_Email");

        builder.HasIndex(i => i.DepartmentId).HasDatabaseName("IX_Interns_DepartmentId");
        builder.HasIndex(i => i.UnitId).HasDatabaseName("IX_Interns_UnitId");
        builder.HasIndex(i => i.MentorId).HasDatabaseName("IX_Interns_MentorId");

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(i => i.DepartmentId)
            .HasConstraintName("FK_Interns_Departments")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .HasConstraintName("FK_Interns_Units")
            .OnDelete(DeleteBehavior.NoAction);

        // Mentor bir ÇALIŞANDIR (Users değil): stajyerin sorumlusu kadrodaki kişidir.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(i => i.MentorId)
            .HasConstraintName("FK_Interns_Employees")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .HasConstraintName("FK_Interns_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
