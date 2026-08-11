using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class AccountRequestConfiguration : IEntityTypeConfiguration<AccountRequest>
{
    public void Configure(EntityTypeBuilder<AccountRequest> builder)
    {
        builder.ToTable("AccountRequests", table =>
        {
            // LeaveRequests ile aynı ilke: talebin öznesi tam olarak biri.
            table.HasCheckConstraint(
                "CK_AccountRequests_Subject",
                "(EmployeeId IS NOT NULL AND InternId IS NULL) OR (EmployeeId IS NULL AND InternId IS NOT NULL)");
        });

        builder.HasKey(a => a.Id).HasName("PK_AccountRequests");

        builder.Property(a => a.Note).HasMaxLength(500);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);

        builder.Property(a => a.ReviewedAt).HasColumnType(AuditColumnConfiguration.DateTimeType);

        builder.ConfigureAuditColumns(a => a.CreatedAt, a => a.UpdatedAt);

        // Aynı kişi için AYNI ANDA yalnızca bir BEKLEYEN talep olabilir; kapanmış
        // talepler kısıtlamaz. Filtre (Status = 1) tam olarak bunu ifade ediyor —
        // filtresiz unique index, kişi için ikinci kez hesap talebi açmayı
        // sonsuza kadar yasaklardı.
        builder.HasIndex(a => a.EmployeeId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [EmployeeId] IS NOT NULL")
            .HasDatabaseName("UX_AccountRequests_PendingEmployee");

        builder.HasIndex(a => a.InternId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [InternId] IS NOT NULL")
            .HasDatabaseName("UX_AccountRequests_PendingIntern");

        builder.HasIndex(a => a.Status).HasDatabaseName("IX_AccountRequests_Status");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .HasConstraintName("FK_AccountRequests_Employees")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Intern>()
            .WithMany()
            .HasForeignKey(a => a.InternId)
            .HasConstraintName("FK_AccountRequests_Interns")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.RequestedByUserId)
            .HasConstraintName("FK_AccountRequests_RequestedBy")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ReviewedByUserId)
            .HasConstraintName("FK_AccountRequests_ReviewedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
