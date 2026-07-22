/* =============================================================================
   HRManagement — GELİŞTİRME ortamı başlangıç verisi

   ⚠️  Bu dosya CANLI ortamda çalıştırılmaz. Şifre bilinçli olarak zayıftır
       ve hash'i herkese açık bir repoda durmaktadır.

   Neden gerekli (tavuk-yumurta): hesapları yalnızca giriş yapmış yetkililer
   açabiliyor, ama Users tablosu boşken kimse giriş yapamaz. İlk hesabın
   uygulamanın dışından konması gerekir.

   Alternatif: API açılışta da aynı işi yapar (API/Seeding/AdminSeeder.cs);
   o yol şifreyi user-secrets'tan okur ve yalnızca tablo BOŞSA çalışır.
   ============================================================================= */

USE HRManagementDb;
GO

/* --- Test admin ------------------------------------------------------------
   Kullanıcı adı : test
   E-posta       : test@test.com
   Şifre         : 12341234

   Hash, projedeki PasswordHasher ile birebir aynı çağrıyla üretildi:
       BCrypt.Net.BCrypt.HashPassword("12341234")     → work factor 11
   BCrypt her çağrıda rastgele salt ürettiği için bu değeri yeniden
   üretemezsin; aynı şifre her seferinde farklı hash verir. Salt hash'in
   içinde taşındığı için ayrı bir sütuna gerek yoktur.                        */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'test@test.com' OR Username = 'test')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, Role, IsActive)
    VALUES ('test',
            'test@test.com',
            '$2a$11$pk4mPVyeCXrW/gghk.JkzO8nJ0Q49Rx1susTqLwneOMBTwesrrzpW',
            1,   -- Role.Admin
            1);  -- IsActive
END
GO

/* --- Örnek departman ------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Yazılım')
BEGIN
    INSERT INTO dbo.Departments (Name, Description)
    VALUES (N'Yazılım', N'Yazılım geliştirme departmanı');
END
GO

/* --- Kontrol -------------------------------------------------------------- */
SELECT Id, Username, Email, Role, IsActive,
       LEN(PasswordHash) AS HashUzunlugu   -- 60 bekleniyor
FROM dbo.Users;

SELECT Id, Name, Description FROM dbo.Departments;
GO
