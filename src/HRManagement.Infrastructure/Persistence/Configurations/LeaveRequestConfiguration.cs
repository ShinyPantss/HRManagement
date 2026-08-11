using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests", table =>
        {
            // Talep sahibi ya çalışan ya stajyerdir — tam olarak biri. Kuralı
            // veritabanı GARANTİ eder; uygulama dışından ya da hatalı bir kod
            // yolundan bozuk kayıt giremez.
            table.HasCheckConstraint(
                "CK_LeaveRequests_Requester",
                "(EmployeeId IS NOT NULL AND InternId IS NULL) OR (EmployeeId IS NULL AND InternId IS NOT NULL)");
        });

        builder.HasKey(l => l.Id).HasName("PK_LeaveRequests");

        builder.Property(l => l.StartDate).HasColumnType(AuditColumnConfiguration.DateType);
        builder.Property(l => l.EndDate).HasColumnType(AuditColumnConfiguration.DateType);

        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.MedicalReport).HasMaxLength(500);
        builder.Property(l => l.RejectionReason).HasMaxLength(500);

        builder.Property(l => l.ManagerApprovedAt).HasColumnType(AuditColumnConfiguration.DateTimeType);
        builder.Property(l => l.HrApprovedAt).HasColumnType(AuditColumnConfiguration.DateTimeType);
        builder.Property(l => l.RejectedAt).HasColumnType(AuditColumnConfiguration.DateTimeType);

        builder.ConfigureAuditColumns(l => l.CreatedAt, l => l.UpdatedAt);

        builder.HasIndex(l => l.EmployeeId).HasDatabaseName("IX_LeaveRequests_EmployeeId");
        builder.HasIndex(l => l.InternId).HasDatabaseName("IX_LeaveRequests_InternId");

        // "Bekleyen talepler" ekranı her açılışta Status'e göre filtreliyor.
        builder.HasIndex(l => l.Status).HasDatabaseName("IX_LeaveRequests_Status");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .HasConstraintName("FK_LeaveRequests_Employees")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Intern>()
            .WithMany()
            .HasForeignKey(l => l.InternId)
            .HasConstraintName("FK_LeaveRequests_Interns")
            .OnDelete(DeleteBehavior.NoAction);

        // Onaylayan/reddeden hep Users'a bakar, Employees'e değil: onaylayan İK
        // uzmanının çalışan kaydı olmayabilir ama hesabı mutlaka vardır.
        // İki aşama AYRI sütun çiftlerinde izlenir — tek bir "ReviewedBy" alanı
        // "aynı kişi iki aşamayı da onaylamasın" kuralını denetlemeye yetmezdi.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.ManagerApprovedByUserId)
            .HasConstraintName("FK_LeaveRequests_ManagerApprovedBy")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.HrApprovedByUserId)
            .HasConstraintName("FK_LeaveRequests_HrApprovedBy")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.RejectedByUserId)
            .HasConstraintName("FK_LeaveRequests_RejectedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
