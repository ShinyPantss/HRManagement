using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HRManagement.Infrastructure.Persistence;

/// <summary>
/// Güncellenen her kaydın UpdatedAt sütununu damgalar.
///
/// Neden interceptor: eskiden bu iş elle yazılan her UPDATE cümlesinde
/// "UpdatedAt = SYSUTCDATETIME()" satırıyla yapılıyordu — yani 12 repository'de
/// tekrar eden ve yeni bir UPDATE yazarken unutulabilecek bir kural. Burada tek
/// yerde durur ve unutulamaz.
///
/// Neden DateTime.UtcNow (veritabanı saati değil): DB saatinde ısrar etmek her
/// tabloya trigger kurmayı ve EF'e ToTable(t => t.HasTrigger(...)) bildirmeyi
/// gerektirirdi (aksi hâlde SQL Server sağlayıcısının OUTPUT optimizasyonu
/// bozulur). Eski koddaki "istemci saatine güvenilmez" uyarısı TARAYICIYI
/// kastediyordu; API sunucusunun saati zaten NTP ile senkron ve CreatedAt hâlâ
/// veritabanından geliyor. On trigger'lık bakım yükü bu farkı hak etmiyor.
///
/// Entity'lerin ortak bir arayüzü (IAuditable gibi) YOK — olsaydı Domain'e
/// dokunmak gerekirdi. Onun yerine EF'in kendi metadata'sına soruluyor:
/// "bu entity'nin UpdatedAt diye bir property'si var mı?"
/// </summary>
public sealed class UpdatedAtInterceptor : SaveChangesInterceptor
{
    private const string UpdatedAtPropertyName = "UpdatedAt";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampUpdatedAt(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampUpdatedAt(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampUpdatedAt(DbContext? context)
    {
        if (context is null)
            return;

        // DetectChanges AÇIKÇA çağrılıyor: interceptor, EF'in kendi değişiklik
        // tespitinden ÖNCE çalışabiliyor. Çağrılmasaydı henüz "Modified" işareti
        // konmamış entity'ler bu döngüde Unchanged görünür ve damgalanmadan geçerdi.
        context.ChangeTracker.DetectChanges();

        // Tek bir "şimdi" değeri: aynı SaveChanges içinde güncellenen kayıtların
        // hepsi aynı damgayı taşısın, milisaniye farkıyla ayrışmasınlar.
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
                continue;

            // UpdatedAt'i olmayan (ileride eklenebilecek) entity'ler sessizce atlanır.
            var property = entry.Metadata.FindProperty(UpdatedAtPropertyName);

            if (property is null || property.ClrType != typeof(DateTime?))
                continue;

            entry.Property(UpdatedAtPropertyName).CurrentValue = now;
        }
    }
}
