/* ============================================================================
   20 — Employees.NationalId: benzersizlik + format kısıtı

   NEDEN
   T.C. Kimlik No hiçbir yerde denetlenmiyordu: ne validator'da (0 kural), ne
   veritabanında (ne UNIQUE ne CHECK). Aynı T.C. ile ikinci bir çalışan kaydı
   açılabiliyordu. Bu yalnızca "kirli veri" değil, bir İŞ KURALI ihlalidir:
   izin bakiyesi çalışan KAYDI başına hesaplandığı için mükerrer kayıt kişinin
   hakkını ikiye böler ve her iki kayıttan ayrı ayrı izin kullanılabilir.

   Uygulama tarafına da kural eklendi (Create/UpdateEmployeeCommandValidator →
   11 hane rakam; handler → GetByNationalIdAsync ön kontrolü). Ama uygulama
   kontrolü "önce SELECT sonra INSERT" olduğu için eşzamanlı iki istek arasından
   sıyrılabilir ve uygulama dışından girilen kayıt kuralı hiç görmez.
   Asıl garantiyi kısıt verir — 03_fixes.sql'deki e-posta kısıtıyla aynı gerekçe.

   NEDEN FILTERED INDEX (UNIQUE constraint değil)
   NationalId NULL olabilir ve NULL kalması meşrudur (alan zorunlu değil).
   SQL Server'da UNIQUE constraint NULL'ları birbirinin AYNISI sayar: ikinci
   "T.C.'si girilmemiş" çalışan eklenemezdi. Filtered index (WHERE ... IS NOT
   NULL) yalnızca DOLU değerleri denetler.
   ============================================================================ */

SET NOCOUNT ON;


/* --- 1) Boş dizeleri NULL'a çek ---------------------------------------------
   Uygulama artık boş T.C.'yi NULL olarak yazıyor (handler'da Trim + boşsa
   null). Eski kayıtlarda '' kalmış olabilir; '' bir DEĞERDİR ve filtered
   index'te birbirleriyle çakışır. Idempotent: ikinci çalıştırmada 0 satır.   */
IF EXISTS (SELECT 1 FROM dbo.Employees WHERE NationalId IS NOT NULL AND LTRIM(RTRIM(NationalId)) = '')
BEGIN
    UPDATE dbo.Employees
    SET NationalId = NULL
    WHERE NationalId IS NOT NULL AND LTRIM(RTRIM(NationalId)) = '';

    PRINT CONCAT('20: ', @@ROWCOUNT, ' kayitta bos T.C. degeri NULL yapildi.');
END
ELSE
    PRINT '20: bos T.C. degeri yok, atlandi.';
GO


/* --- 2) Employees.NationalId → filtered UNIQUE index -------------------------
   Engelleyici veri varsa sessizce geçmez: ne yapılması gerektiğini yazıp durur
   (db/README.md'deki kural).                                                  */
IF EXISTS (SELECT NationalId FROM dbo.Employees
           WHERE NationalId IS NOT NULL
           GROUP BY NationalId HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('ATLANDI: Employees.NationalId sutununda tekrar eden degerler var. Kontrol: SELECT NationalId, COUNT(*) FROM dbo.Employees WHERE NationalId IS NOT NULL GROUP BY NationalId HAVING COUNT(*) > 1;', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Employees_NationalId')
BEGIN
    CREATE UNIQUE INDEX UX_Employees_NationalId
        ON dbo.Employees (NationalId)
        WHERE NationalId IS NOT NULL;

    PRINT 'OK: UX_Employees_NationalId eklendi';
END
ELSE
    PRINT 'ATLANDI: UX_Employees_NationalId zaten var';
GO


/* --- 3) Employees.NationalId → format CHECK'i --------------------------------
   11 hane, yalnızca rakam. LIKE deseni: tam 11 rakam ve fazlası yok
   ([0-9] on bir kez + "rakam olmayan karakter içermesin").
   Uygulamadaki Matches("^[0-9]{11}$") kuralının veritabanı karşılığı.         */
IF EXISTS (SELECT 1 FROM dbo.Employees
           WHERE NationalId IS NOT NULL
             AND NationalId NOT LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
BEGIN
    RAISERROR('ATLANDI: Employees.NationalId sutununda 11 haneli rakam olmayan degerler var. Kontrol: SELECT Id, NationalId FROM dbo.Employees WHERE NationalId IS NOT NULL AND NationalId NOT LIKE ''[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'';', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'CK_Employees_NationalId')
BEGIN
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_NationalId
        CHECK (NationalId IS NULL
               OR NationalId LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');

    PRINT 'OK: CK_Employees_NationalId eklendi';
END
ELSE
    PRINT 'ATLANDI: CK_Employees_NationalId zaten var';
GO
