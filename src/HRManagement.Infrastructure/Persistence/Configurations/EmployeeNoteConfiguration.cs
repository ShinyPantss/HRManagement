using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeNoteConfiguration : IEntityTypeConfiguration<EmployeeNote>
{
    public void Configure(EntityTypeBuilder<EmployeeNote> builder)
    {
        builder.ToTable("EmployeeNotes");

        builder.HasKey(n => n.Id).HasName("PK_EmployeeNotes");

        builder.Property(n => n.Content).HasMaxLength(1000).IsRequired();

        builder.ConfigureAuditColumns(n => n.CreatedAt, n => n.UpdatedAt);

        builder.HasIndex(n => n.EmployeeId).HasDatabaseName("IX_EmployeeNotes_EmployeeId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(n => n.EmployeeId)
            .HasConstraintName("FK_EmployeeNotes_Employees")
            .OnDelete(DeleteBehavior.NoAction);

        // Notu YAZAN Users'a bağlanır, Employees'e değil: "bu işlemi kim yaptı"
        // sorusunun cevabı her zaman hesaptır.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.AuthorUserId)
            .HasConstraintName("FK_EmployeeNotes_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
