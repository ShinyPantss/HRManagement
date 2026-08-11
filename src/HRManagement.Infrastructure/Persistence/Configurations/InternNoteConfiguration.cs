using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class InternNoteConfiguration : IEntityTypeConfiguration<InternNote>
{
    public void Configure(EntityTypeBuilder<InternNote> builder)
    {
        builder.ToTable("InternNotes");

        builder.HasKey(n => n.Id).HasName("PK_InternNotes");

        builder.Property(n => n.Content).HasMaxLength(1000).IsRequired();

        builder.ConfigureAuditColumns(n => n.CreatedAt, n => n.UpdatedAt);

        builder.HasIndex(n => n.InternId).HasDatabaseName("IX_InternNotes_InternId");

        builder.HasOne<Intern>()
            .WithMany()
            .HasForeignKey(n => n.InternId)
            .HasConstraintName("FK_InternNotes_Interns")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.AuthorUserId)
            .HasConstraintName("FK_InternNotes_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
