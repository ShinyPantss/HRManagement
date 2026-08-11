using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id).HasName("PK_Users");

        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(100).IsRequired();

        // BCrypt hash'i 60 karakter. Sütun dar olursa hash SESSİZCE kırpılır ve
        // giriş hiç çalışmaz — bulması çok zor bir hata (bkz. db/05_full_setup.sql).
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

        // Role enum'ı int olarak saklanır — EF'in varsayılan davranışı, ek eşleme
        // gerekmiyor. Sayı karşılıkları Domain/Enums'ta tanımlı.

        // IsActive'in veritabanı default'u (1) BİLİNÇLİ OLARAK modellenmedi.
        // Nedeni aşağıda, EmployeeConfiguration'da tek yerde açıklanıyor.

        builder.ConfigureAuditColumns(u => u.CreatedAt, u => u.UpdatedAt);

        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("UQ_Users_Username");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
    }
}
