/* =============================================================================
   HRManagement — Canlı izin seed'i: şu an süren + yaklaşan        (2026-07-29)

   NEDEN AYRI DOSYA
     16_seed_leave_history.sql adı üstünde GEÇMİŞ üretir: tüm pencereleri
     30-450 gün ÖNCESİNE bakar. Bu yüzden panodaki "Şu an izinde olanlar" ve
     "Yaklaşan izinler" kartları boş kalıyor — kod değil, veri eksikliği.
     Bu script o iki kartı besleyen az sayıda kaydı ekler.

   ÇALIŞTIRMA SIRASI: ... → 16_seed_leave_history.sql → BU DOSYA.
     16'dan SONRA çalışmalı: kalan izin hakkını hesaplarken 16'nın ürettiği
     kullanımı da sayar, böylece ikisi birlikte hakkı aşmaz.

   BİLİNÇLİ KARARLAR
     1) AYRI İMZA. Kayıtlar '[seed-live]' önekiyle işaretlenir. 16'nın temizliği
        '[seed]%' desenini sildiği için bu kayıtlara DOKUNMAZ; ikisi birbirinden
        bağımsız çalıştırılabilir.

     2) AZ KİŞİ. Şirketin yarısı aynı anda izinde olmaz. Kayıtlar Rn (sıra no)
        modülüne göre küçük bir azınlığa verilir — pano gerçekçi görünsün.

     3) BÜTÇE KORUNUR. Yıllık izin yalnızca kalan hakkı yeterli olanlara yazılır;
        yetersizse o kişi ATLANIR (kısaltılmaz). Hastalık izni haktan düşmediği
        için serbesttir ve "şu an izinde" çeşitliliğini o taşır.

     4) HAFTA SONU KAPSAMI. "Şu an izinde" kaydı bu haftanın Pazartesisinden
        başlar ve 8 iş günü sürer (bu hafta + gelecek haftanın başı). 5 iş günü
        olsaydı Cuma biterdi ve script Cumartesi çalıştırıldığında kart yine
        boş görünürdü.

   GERİ ALMA
     DELETE FROM dbo.LeaveRequests WHERE Description LIKE '[[]seed-live]%';
   ============================================================================= */

SET NOCOUNT ON;
GO

USE HRManagementDb;
GO

/* --- Ön koşul -------------------------------------------------------------- */
IF COL_LENGTH('dbo.LeaveRequests', 'WorkingDays') IS NULL
BEGIN
    RAISERROR('DURDURULDU: once db/10_leave_rules.sql calistirilmali.', 16, 1);
    SET NOEXEC ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE IsActive = 1)
BEGIN
    RAISERROR('DURDURULDU: aktif calisan yok. Once db/12_seed_org_kadro.sql calistirilmali.', 16, 1);
    SET NOEXEC ON;
END
GO


/* =============================================================================
   1) TEMİZLİK — yalnızca bu script'in kayıtları
   ============================================================================= */

DELETE FROM dbo.LeaveRequests WHERE Description LIKE '[[]seed-live]%';
PRINT CONCAT('Temizlik: ', @@ROWCOUNT, ' eski canli izin silindi.');
GO


/* =============================================================================
   2) CANLI + YAKLAŞAN İZİNLER

   Şablonlar (WeekOffset: 0 = bu hafta, +1 = gelecek hafta, +2 = iki hafta sonra):
     L1  Yıllık   8 iş günü, bu hafta başlar   → ŞU AN İZİNDE  (Rn % 9 = 0)
     L2  Hastalık 3 iş günü, bu hafta başlar   → ŞU AN İZİNDE  (Rn % 9 = 4)
     L3  Yıllık   5 iş günü, gelecek hafta     → YAKLAŞAN      (Rn % 7 = 1)
     L4  Yıllık   3 iş günü, iki hafta sonra   → YAKLAŞAN      (Rn % 7 = 3)
   ============================================================================= */

DECLARE @ManagerUserId int = ISNULL(
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 3 AND IsActive = 1 ORDER BY Id),
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id));

DECLARE @HrUserId int = ISNULL(
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 2 AND IsActive = 1 ORDER BY Id),
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id));

DECLARE @Today date = CAST(GETDATE() AS date);

-- Bu haftanın Pazartesisi. 1900-01-01 bir Pazartesi olduğu için
-- (DATEDIFF % 7) = 0..6 → Pzt..Paz; farkı geri çıkarınca hafta başı gelir.
-- (16_seed_leave_history.sql ile aynı formül — dile ve @@DATEFIRST'e bağlı değil.)
DECLARE @ThisMonday date =
    DATEADD(DAY, -(DATEDIFF(DAY, '19000101', @Today) % 7), @Today);

;WITH Staff AS
(
    SELECT
        e.Id AS EmployeeId,
        e.HireDate,
        ISNULL(e.AnnualLeaveDays, 14) AS Entitlement,
        -- 16'nın kayıtları DAHİL mevcut yıllık kullanım.
        ISNULL((SELECT SUM(l.WorkingDays)
                FROM dbo.LeaveRequests l
                WHERE l.EmployeeId = e.Id
                  AND l.Type = 1
                  AND l.Status IN (1, 2, 3)), 0) AS AlreadyUsed,
        ROW_NUMBER() OVER (ORDER BY e.DepartmentId, e.Id) AS Rn
    FROM dbo.Employees e
    WHERE e.IsActive = 1
),
LivePlan AS
(
    -- L1) ŞU AN İZİNDE — yıllık, 8 iş günü (bu hafta + gelecek hafta başı).
    SELECT s.EmployeeId, 1 AS Type, 8 AS SpanWorkDays, 0 AS WeekOffset, N'Yıllık izin' AS Note
    FROM Staff s
    WHERE s.Rn % 9 = 0
      AND s.Entitlement - s.AlreadyUsed >= 8      -- hak yetmiyorsa ATLA

    UNION ALL
    -- L2) ŞU AN İZİNDE — hastalık, 3 iş günü. Yıllık haktan düşmez.
    SELECT s.EmployeeId, 3, 3, 0, N'Hastalık izni'
    FROM Staff s
    WHERE s.Rn % 9 = 4

    UNION ALL
    -- L3) YAKLAŞAN — yıllık, gelecek hafta, 5 iş günü.
    SELECT s.EmployeeId, 1, 5, 1, N'Yıllık izin'
    FROM Staff s
    WHERE s.Rn % 7 = 1
      AND s.Entitlement - s.AlreadyUsed >= 5

    UNION ALL
    -- L4) YAKLAŞAN — yıllık, iki hafta sonra, 3 iş günü.
    SELECT s.EmployeeId, 1, 3, 2, N'Yıllık izin'
    FROM Staff s
    WHERE s.Rn % 7 = 3
      AND s.Entitlement - s.AlreadyUsed >= 3
),
Deduped AS
(
    -- KİŞİ BAŞINA TEK KAYIT. Şablon koşulları (Rn % 9, Rn % 7) aynı kişiye
    -- denk gelebilir — ör. Rn = 36 hem L1 hem L3 üretir ve tarihleri örtüşür.
    -- Aşağıdaki NOT EXISTS bunu yakalayamaz: o kontrol veritabanındaki MEVCUT
    -- satırlara bakar, aynı INSERT içinde üretilen kardeş satırları göremez.
    -- En erken başlayanı (WeekOffset küçük) tutuyoruz.
    SELECT p.*, ROW_NUMBER() OVER (PARTITION BY p.EmployeeId ORDER BY p.WeekOffset, p.Type) AS Pick
    FROM LivePlan p
),
Ranged AS
(
    SELECT
        p.EmployeeId,
        p.Type,
        p.Note,
        DATEADD(WEEK, p.WeekOffset, @ThisMonday) AS StartDate,
        -- İş günü → takvim günü: her tam 5 iş günü bir hafta sonu (2 gün) atlar.
        -- 8 iş günü → 8-1 = 7 takvim + (7/5)*2 = 2 → Pazartesi + 9 gün (Çarşamba).
        DATEADD(DAY,
            (p.SpanWorkDays - 1) + ((p.SpanWorkDays - 1) / 5) * 2,
            DATEADD(WEEK, p.WeekOffset, @ThisMonday)) AS EndDate
    FROM Deduped p
    WHERE p.Pick = 1
)
INSERT INTO dbo.LeaveRequests
(
    EmployeeId, InternId, Type, StartDate, EndDate, WorkingDays,
    Description, MedicalReport, Status,
    ManagerApprovedByUserId, ManagerApprovedAt,
    HrApprovedByUserId, HrApprovedAt,
    CreatedAt
)
SELECT
    r.EmployeeId,
    NULL,
    r.Type,
    r.StartDate,
    r.EndDate,
    wd.WorkingDays,
    CONCAT(N'[seed-live] ', r.Note),
    CASE WHEN r.Type = 3
         THEN CONCAT(N'Rapor No: ', FORMAT(r.EmployeeId, '0000'), '-',
                     FORMAT(r.StartDate, 'yyyyMMdd'), N' · Aile Hekimliği')
         ELSE NULL END,
    3,                                                   -- Approved
    -- Onay izi GEÇMİŞTE olmalı: yaklaşan izin de önceden onaylanmıştır.
    @ManagerUserId, DATEADD(DAY, -9,  CAST(@Today AS datetime2(0))),
    @HrUserId,      DATEADD(DAY, -7,  CAST(@Today AS datetime2(0))),
    DATEADD(DAY, -12, CAST(@Today AS datetime2(0)))
FROM Ranged r
CROSS APPLY
(
    SELECT COUNT(*) AS WorkingDays
    FROM
    (
        SELECT TOP (DATEDIFF(DAY, r.StartDate, r.EndDate) + 1)
               DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, r.StartDate) AS D
        FROM sys.all_objects
    ) g
    WHERE (DATEDIFF(DAY, '19000101', g.D) % 7) < 5      -- 0..4 = Pzt..Cum
) wd
WHERE r.StartDate >= (SELECT e.HireDate FROM dbo.Employees e WHERE e.Id = r.EmployeeId)
  -- Aynı kişide çakışan izin üretme: 16'nın kayıtları geçmişte olduğu için
  -- normalde çakışmaz, ama script tekrar tekrar çalıştırılabildiği için kontrol şart.
  AND NOT EXISTS (
        SELECT 1 FROM dbo.LeaveRequests x
        WHERE x.EmployeeId = r.EmployeeId
          AND x.Status <> 4
          AND x.StartDate <= r.EndDate
          AND r.StartDate <= x.EndDate);

PRINT CONCAT('Eklendi: ', @@ROWCOUNT, ' canli/yaklasan izin.');
GO


/* =============================================================================
   3) DOĞRULAMA — panodaki iki kartın göreceği veri
   ============================================================================= */

DECLARE @Today date = CAST(GETDATE() AS date);

PRINT '';
PRINT '--- SU AN IZINDE (pano karti) ---';

SELECT e.FirstName + ' ' + e.LastName AS Kisi,
       CASE l.Type WHEN 1 THEN N'Yıllık' WHEN 2 THEN N'Ücretsiz' ELSE N'Hastalık' END AS Tur,
       l.StartDate, l.EndDate, l.WorkingDays
FROM dbo.LeaveRequests l
JOIN dbo.Employees e ON e.Id = l.EmployeeId
WHERE l.Status = 3 AND l.StartDate <= @Today AND l.EndDate >= @Today
ORDER BY l.EndDate;

PRINT '--- YAKLASAN IZINLER (onumuzdeki 14 gun) ---';

SELECT e.FirstName + ' ' + e.LastName AS Kisi,
       CASE l.Type WHEN 1 THEN N'Yıllık' WHEN 2 THEN N'Ücretsiz' ELSE N'Hastalık' END AS Tur,
       l.StartDate, l.EndDate, l.WorkingDays,
       DATEDIFF(DAY, @Today, l.StartDate) AS KacGunSonra
FROM dbo.LeaveRequests l
JOIN dbo.Employees e ON e.Id = l.EmployeeId
WHERE l.Status = 3
  AND l.StartDate > @Today
  AND l.StartDate <= DATEADD(DAY, 14, @Today)
ORDER BY l.StartDate;

PRINT '--- Yillik hakki asan (BOS OLMALI) ---';

SELECT e.Id, e.FirstName + ' ' + e.LastName AS Calisan,
       ISNULL(e.AnnualLeaveDays, 14) AS Hak,
       SUM(l.WorkingDays) AS Kullanilan
FROM dbo.LeaveRequests l
JOIN dbo.Employees e ON e.Id = l.EmployeeId
WHERE l.Type = 1 AND l.Status IN (1, 2, 3)
GROUP BY e.Id, e.FirstName, e.LastName, e.AnnualLeaveDays
HAVING SUM(l.WorkingDays) > ISNULL(e.AnnualLeaveDays, 14);
GO

PRINT '';
PRINT 'OK: Canli izin seed i tamamlandi.';
GO

SET NOEXEC OFF;
GO
