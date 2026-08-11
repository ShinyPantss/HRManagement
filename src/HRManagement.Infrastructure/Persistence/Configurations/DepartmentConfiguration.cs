using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id).HasName("PK_Departments");

        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.ConfigureAuditColumns(d => d.CreatedAt, d => d.UpdatedAt);
    }
}
