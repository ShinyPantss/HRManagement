using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class InternTaskConfiguration : IEntityTypeConfiguration<InternTask>
{
    public void Configure(EntityTypeBuilder<InternTask> builder)
    {
        builder.ToTable("InternTasks");

        builder.HasKey(t => t.Id).HasName("PK_InternTasks");

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);

        builder.Property(t => t.DueDate).HasColumnType(AuditColumnConfiguration.DateType);

        builder.ConfigureAuditColumns(t => t.CreatedAt, t => t.UpdatedAt);

        builder.HasIndex(t => t.InternId).HasDatabaseName("IX_InternTasks_InternId");

        builder.HasOne<Intern>()
            .WithMany()
            .HasForeignKey(t => t.InternId)
            .HasConstraintName("FK_InternTasks_Interns")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .HasConstraintName("FK_InternTasks_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
