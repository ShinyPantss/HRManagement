using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Her tabloda tekrar eden CreatedAt / UpdatedAt eşlemesi. On entity'de
/// aynı dört satırı yazmak yerine tek yerde tanımlanır.
/// </summary>
internal static class AuditColumnConfiguration
{
    /// <summary>SQL Server'daki sütun tipi: datetime2(0) — saniye hassasiyeti yeter, 8 yerine 6 bayt.</summary>
    public const string DateTimeType = "datetime2(0)";

    /// <summary>Tarih-saat taşımayan alanlar (doğum, işe giriş, izin başlangıcı) için.</summary>
    public const string DateType = "date";

    public static void ConfigureAuditColumns<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> createdAt,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime?>> updatedAt)
        where TEntity : class
    {
        // CreatedAt'i UYGULAMA YAZMAZ, veritabanı default'u doldurur (SYSUTCDATETIME).
        // Neden: saat tek kaynaktan gelsin — uygulama sunucusunun saati kaysa bile
        // kayıt zamanları kendi aralarında tutarlı kalır. ValueGeneratedOnAdd sayesinde
        // EF sütunu INSERT'e hiç koymaz ve üretilen değeri geri okur.
        builder.Property(createdAt)
            .HasColumnType(DateTimeType)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        // UpdatedAt null kalır kayıt hiç güncellenmediyse — CreatedAt ile aynı değere
        // set etmek "hiç değişmedi" bilgisini yok ederdi (bkz. db/README.md).
        builder.Property(updatedAt)
            .HasColumnType(DateTimeType);
    }
}
