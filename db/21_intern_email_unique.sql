/* ============================================================================
   21 — Interns.Email: benzersizlik kısıtı

   NEDEN
   Çalışan tarafında e-posta benzersizliği ÜÇ katmanla korunuyor: veritabanı
   kısıtı (UQ_Employees_Email, 03_fixes.sql), handler'daki ön kontrol ve
   güncellemede "kendi kaydı hariç" karşılaştırması. Stajyer tarafında bunların
   HİÇBİRİ yoktu — aynı e-posta ile istediğiniz kadar stajyer açılabiliyordu.

   E-posta yalnızca bir iletişim alanı değil: hesap açma akışının kimlik
   anahtarıdır (Users.Email zaten UNIQUE). Mükerrer stajyer e-postası "bu adres
   kime ait?" sorusunu belirsizleştirir.

   Uygulama tarafı bu betikle birlikte tamamlandı (CreateInternCommandHandler +
   UpdateInternCommandHandler → GetByEmailAsync ön kontrolü). Ama uygulama
   kontrolü "önce SELECT sonra INSERT" olduğu için eşzamanlı iki istek arasından
   sıyrılabilir; asıl garantiyi kısıt verir.

   NEDEN UNIQUE CONSTRAINT (filtered index değil)
   Interns.Email NOT NULL'dır — NULL sorunu yok, e-postadaki çalışan kısıtıyla
   birebir aynı biçim kullanılabilir.
   ============================================================================ */

SET NOCOUNT ON;

IF EXISTS (SELECT Email FROM dbo.Interns GROUP BY Email HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('ATLANDI: Interns.Email sutununda tekrar eden degerler var. Kontrol: SELECT Email, COUNT(*) FROM dbo.Interns GROUP BY Email HAVING COUNT(*) > 1;', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_Interns_Email')
BEGIN
    ALTER TABLE dbo.Interns ADD CONSTRAINT UQ_Interns_Email UNIQUE (Email);
    PRINT 'OK: UQ_Interns_Email eklendi';
END
ELSE
    PRINT 'ATLANDI: UQ_Interns_Email zaten var';
GO
