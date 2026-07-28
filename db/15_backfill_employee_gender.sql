/* =============================================================================
   HRManagement — Mevcut çalışanların Cinsiyet backfill'i               (2026-07-27)

   14_employee_gender.sql kolonu ekledi ama eski kayıtlar NULL. Bu script onları
   ADA GÖRE doldurur. Tek sinyal ad olduğu için bu bir SEZGİDİR (gerçek hayatta
   cinsiyet kişiden alınır); demo/seed verisi için pratik ve denetlenebilir yol.

   GÜVENLİ + TEKRARLANABİLİR:
     - Yalnızca Gender IS NULL satırlara dokunur; elle/uygulamadan girilmişi EZMEZ.
     - Ad haritası, 12_seed_org_kadro.sql'deki gerçek adlardan çıkarıldı.
     - Eşleşmeyen / unisex adlar EN SONDA raporlanır — onları sen karara bağlarsın
       (uydurmuyoruz: Deniz / Özgür / Ömür gibi unisex adlar gerçek kişiler).

   Gender: 1 = Erkek, 2 = Kadın.
   ============================================================================= */

USE HRManagementDb;
GO

;WITH gmap(Name, G) AS
(
    SELECT * FROM (VALUES
        -- ── Kadın (2) ──
        (N'Ayşe',2),(N'Elif',2),(N'Zeynep',2),(N'Selin',2),(N'Merve',2),(N'Derya',2),
        (N'Ebru',2),(N'Pınar',2),(N'Gizem',2),(N'Nazlı',2),
        (N'Nalan',2),(N'Öznur',2),(N'Mihrigül',2),(N'Aslı',2),(N'Gamze',2),(N'Duygu',2),
        (N'Hülya Arife',2),(N'Aleyna',2),(N'Ceren',2),(N'Esra',2),(N'Filiz',2),
        (N'Günnur',2),(N'Hazal',2),(N'Nergis',2),

        -- ── Erkek (1) ──
        (N'Ahmet',1),(N'Mehmet',1),(N'Mustafa',1),(N'Emre',1),(N'Burak',1),(N'Kerem',1),
        (N'Volkan',1),(N'Tolga',1),(N'Cem',1),(N'Barış',1),
        (N'Ali',1),(N'Aytaç',1),(N'Ekrem',1),(N'Ömer',1),(N'Serkan',1),(N'Gökhan',1),
        (N'Onur',1),(N'Soner',1),(N'Turgay',1),(N'Burç',1),(N'Orçun',1),(N'Yasin',1),
        (N'Alper',1),(N'Murat',1),(N'Çağlar',1),(N'Semih',1),(N'Mücahit',1),

        -- ── Unisex adlar — kullanıcı kararı (her ad seed'de tek kişiye denk gelir) ──
        (N'Deniz',1),   -- Deniz Paksoy Altınok → Erkek
        (N'Özgür',1),   -- Özgür Ergün        → Erkek
        (N'Ömür',2)     -- Ömür Mavuş         → Kadın
    ) v(Name, G)
)
UPDATE e
SET e.Gender = m.G,
    e.UpdatedAt = SYSUTCDATETIME()
FROM dbo.Employees e
JOIN gmap m ON m.Name = e.FirstName
WHERE e.Gender IS NULL;

PRINT CONCAT('OK: ', @@ROWCOUNT, ' calisanin cinsiyeti ada gore dolduruldu.');
GO

/* --- DOĞRULAMA: hâlâ cinsiyeti boş olanlar (bu adları haritaya ekle ya da
       Email ile tek satır UPDATE et). 0 satır = hepsi doldu. --- */
SELECT FirstName, COUNT(*) AS BosAdet
FROM dbo.Employees
WHERE Gender IS NULL
GROUP BY FirstName
ORDER BY BosAdet DESC, FirstName;
GO

/* --- Özet: cinsiyet dağılımı --- */
SELECT
    SUM(CASE WHEN Gender = 1 THEN 1 ELSE 0 END) AS Erkek,
    SUM(CASE WHEN Gender = 2 THEN 1 ELSE 0 END) AS Kadin,
    SUM(CASE WHEN Gender IS NULL THEN 1 ELSE 0 END) AS Bos
FROM dbo.Employees;
GO
