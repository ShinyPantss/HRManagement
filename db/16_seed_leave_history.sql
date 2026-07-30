/* =============================================================================
   HRManagement — İzin geçmişi seed'i (onaylı)                       (2026-07-29)

   NE YAPAR
     Aktif çalışanların her birine, GEÇMİŞE dönük, iki aşaması da tamamlanmış
     (Status = 3 Approved) izin kayıtları üretir. Amaç "İzin Geçmişi" ekranını
     gerçekçi hacimde veriyle görebilmek.

   ÇALIŞTIRMA SIRASI: 05_full_setup.sql → 11_units.sql → 12_seed_org_kadro.sql
                      → 10_leave_rules.sql → BU DOSYA.

   BİLİNÇLİ KARARLAR
     1) YILLIK İZİN BÜTÇEYE UYAR. GetTotalUsedAnnualDaysAsync kümülatif çalışır
        (dönem filtresi YOK): çalışanın TÜM zamanlardaki yıllık izin iş günleri
        AnnualLeaveDays hakkına karşı toplanır. Seed rastgele gün verseydi
        bakiyeler eksiye düşer, kimse yeni talep açamazdı. Bu yüzden yıllık
        izinler kalan hakkın YARISINI aşmayacak şekilde ölçeklenir.
        Hacmi hastalık ve ücretsiz izinler taşır — onlar yıllık haktan düşmez.

     2) İŞ GÜNÜ SQL'DE HESAPLANIR. WorkingDays uygulamada C# ile hesaplanıp
        saklanıyor; seed veriyi doğrudan yazdığı için aynı sayımı burada
        yapmak zorunda. Hafta sonu tespiti DATENAME/DATEPART ile DEĞİL,
        sabit bir referans günle yapılır: 1900-01-01 bir Pazartesi'dir, bu
        yüzden (DATEDIFF(DAY,'19000101', d) % 7) daima 0=Pzt … 5=Cmt, 6=Paz
        döner. DATENAME dile, DATEPART ise @@DATEFIRST ayarına bağlıdır;
        ikisi de sunucudan sunucuya değişir, bu formül değişmez.

     3) BAŞLANGIÇLAR PAZARTESİYE HİZALANIR. Gerçek hayatta izinler genelde
        hafta başında başlar; ayrıca hafta sonuna denk gelen "0 iş günlük"
        anlamsız kayıtlar oluşmaz.

     4) ONAY İZİ DOLDURULUR. Onaylı bir talebin onaylayanı belli olmalı, aksi
        halde detay ekranında boş bir onay zinciri görünür. İki aşama FARKLI
        hesaplara yazılır (uygulama kuralı: aynı kişi iki aşamayı da onaylayamaz).

     5) İDEMPOTENT. Ürettiği satırlar Description'daki '[seed]' önekinden
        tanınır; tekrar çalıştırıldığında önce kendi kayıtlarını siler.
        ELLE girilmiş izinlere DOKUNMAZ.

   GERİ ALMA
     DELETE FROM dbo.LeaveRequests WHERE Description LIKE '[[]seed]%';
   ============================================================================= */

SET NOCOUNT ON;
GO

USE HRManagementDb;
GO

/* --- Ön koşul -------------------------------------------------------------- */
IF COL_LENGTH('dbo.LeaveRequests', 'WorkingDays') IS NULL
   OR COL_LENGTH('dbo.LeaveRequests', 'InternId') IS NULL
BEGIN
    RAISERROR('DURDURULDU: once db/05_full_setup.sql ve db/10_leave_rules.sql calistirilmali.', 16, 1);
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
   1) TEMİZLİK — yalnızca bu seed'in ürettiği satırlar

   '[seed]' öneki bu script'in imzasıdır. LIKE deseninde köşeli parantez
   karakter sınıfı anlamına geldiği için ilk '[' kaçırılır: '[[]seed]%'.
   ============================================================================= */

DELETE FROM dbo.LeaveRequests WHERE Description LIKE '[[]seed]%';
PRINT CONCAT('Temizlik: ', @@ROWCOUNT, ' eski seed izni silindi.');
GO


/* =============================================================================
   2) İZİN PLANI

   Her aktif çalışan için 5 şablon üretilir. Tarihler çalışanın sırasına (Rn)
   göre kaydırılır: aynı gün herkes izinli görünmez, kayıtlar son ~14 aya yayılır.

   Onaylayan hesaplar burada belirlenir (GO ile ayrılmış batch'ler değişken
   paylaşmaz, bu yüzden INSERT ile AYNI batch'te durmalılar). İki aşama farklı
   kişilere yazılır; uygun rol yoksa Admin'e düşülür ve sonda uyarı basılır.

   Şablonlar (Type: 1=Yıllık 2=Ücretsiz 3=Hastalık):
     A  Yıllık  — uzun blok   (bütçeye göre ölçeklenir)
     B  Yıllık  — kısa blok   (bütçeye göre ölçeklenir)
     C  Hastalık— 2 gün       (yıllık haktan düşmez)
     D  Hastalık— 1 gün       (yıllık haktan düşmez)
     E  Ücretsiz— 3 gün       (yıllık haktan düşmez, çalışanların ~1/3'ünde)
   ============================================================================= */

DECLARE @ManagerUserId int = ISNULL(
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 3 AND IsActive = 1 ORDER BY Id),
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id));

DECLARE @HrUserId int = ISNULL(
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 2 AND IsActive = 1 ORDER BY Id),
    (SELECT TOP 1 Id FROM dbo.Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id));

DECLARE @Today date = CAST(GETDATE() AS date);

;WITH Staff AS
(
    SELECT
        e.Id            AS EmployeeId,
        e.DepartmentId,
        e.HireDate,
        -- Hak: tanımsızsa kanuni asgariye yakın bir varsayılan.
        ISNULL(e.AnnualLeaveDays, 14) AS Entitlement,
        -- Seed DIŞI mevcut yıllık kullanım: bütçe bunun üstüne konur, böylece
        -- elle girilmiş izinlerle toplam hakkı aşmayız.
        ISNULL((SELECT SUM(l.WorkingDays)
                FROM dbo.LeaveRequests l
                WHERE l.EmployeeId = e.Id
                  AND l.Type = 1
                  AND l.Status IN (1, 2, 3)), 0) AS AlreadyUsed,
        -- Sıra numarası tarih kaydırmasında kullanılır: departmana göre
        -- sıralandığı için aynı ekiptekiler ardışık Rn alır ve izinleri
        -- takvimde birbirinden uzağa düşer (herkes aynı hafta izinli olmasın).
        ROW_NUMBER() OVER (ORDER BY e.DepartmentId, e.Id) AS Rn
    FROM dbo.Employees e
    WHERE e.IsActive = 1
),
Budget AS
(
    SELECT
        s.*,
        -- Kalan hakkın YARISI: seed geçmiş kullanımı temsil eder, bakiyeyi
        -- tüketmez. Negatife düşmesin diye alt sınır 0.
        CASE WHEN s.Entitlement - s.AlreadyUsed > 0
             THEN (s.Entitlement - s.AlreadyUsed) / 2
             ELSE 0 END AS AnnualBudget
    FROM Staff s
),
LeavePlan AS
(
    -- Her şablonun KENDİ tarih penceresi var ve pencereler örtüşmez:
    --   A 30-90 · B 120-180 · C 210-270 · D 300-360 · E 390-450 gün önce.
    -- Aralarındaki 30 günlük boşluk şart: aynı çalışanın iki izni çakışırsa
    -- uygulamanın "tarih çakışması" kuralıyla tutarsız veri üretmiş olurduk
    -- (en uzun blok 5 iş günü olduğu için 30 gün fazlasıyla yeter).

    -- A) Yıllık — uzun blok: bütçenin 3/5'i, en çok 5 iş günü (Pzt-Cum).
    SELECT b.EmployeeId, 1 AS Type,
           CASE WHEN b.AnnualBudget * 3 / 5 > 5 THEN 5 ELSE b.AnnualBudget * 3 / 5 END AS SpanDays,
           30 + (b.Rn % 6) * 12 AS DaysAgo,
           N'Yıllık izin' AS Note
    FROM Budget b
    WHERE b.AnnualBudget >= 2

    UNION ALL
    -- B) Yıllık — kısa blok: bütçenin kalanı, en çok 3 iş günü.
    SELECT b.EmployeeId, 1,
           CASE WHEN b.AnnualBudget - (CASE WHEN b.AnnualBudget * 3 / 5 > 5 THEN 5 ELSE b.AnnualBudget * 3 / 5 END) > 3
                THEN 3
                ELSE b.AnnualBudget - (CASE WHEN b.AnnualBudget * 3 / 5 > 5 THEN 5 ELSE b.AnnualBudget * 3 / 5 END) END,
           120 + (b.Rn % 6) * 12,
           N'Yıllık izin'
    FROM Budget b
    WHERE b.AnnualBudget >= 4

    UNION ALL
    -- C) Hastalık — 2 gün. Yıllık haktan düşmez, bu yüzden herkese verilebilir.
    SELECT b.EmployeeId, 3, 2, 210 + (b.Rn % 6) * 12, N'Hastalık izni'
    FROM Budget b

    UNION ALL
    -- D) Hastalık — 1 gün, çalışanların yarısına.
    SELECT b.EmployeeId, 3, 1, 300 + (b.Rn % 6) * 12, N'Hastalık izni'
    FROM Budget b
    WHERE b.Rn % 2 = 0

    UNION ALL
    -- E) Ücretsiz — 3 gün, çalışanların yaklaşık üçte birine.
    SELECT b.EmployeeId, 2, 3, 390 + (b.Rn % 6) * 12, N'Ücretsiz izin'
    FROM Budget b
    WHERE b.Rn % 3 = 0
),
Dated AS
(
    SELECT
        p.EmployeeId,
        p.Type,
        p.Note,
        -- Başlangıcı O HAFTANIN PAZARTESİSİNE çek. 1900-01-01 Pazartesi olduğu
        -- için (DATEDIFF % 7) = 0..6 → Pzt..Paz; farkı geri çıkarınca Pazartesi.
        DATEADD(DAY,
            -(DATEDIFF(DAY, '19000101', DATEADD(DAY, -p.DaysAgo, @Today)) % 7),
            DATEADD(DAY, -p.DaysAgo, @Today)) AS StartDate,
        p.SpanDays
    FROM LeavePlan p
    WHERE p.SpanDays >= 1
),
Ranged AS
(
    SELECT
        d.EmployeeId,
        d.Type,
        d.Note,
        d.StartDate,
        -- Pazartesiden başlayıp SpanDays iş günü: 5 günü aşmadığı için
        -- hafta sonuna taşmaz, bitiş = başlangıç + (span - 1).
        DATEADD(DAY, d.SpanDays - 1, d.StartDate) AS EndDate
    FROM Dated d
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
    CONCAT(N'[seed] ', r.Note),
    -- Hastalık izninde rapor ZORUNLU (uygulama kuralı). Seed de doldurur ki
    -- ekranda eksik veri görünmesin.
    CASE WHEN r.Type = 3
         THEN CONCAT(N'Rapor No: ', FORMAT(r.EmployeeId, '0000'), '-',
                     FORMAT(r.StartDate, 'yyyyMMdd'), N' · Aile Hekimliği')
         ELSE NULL END,
    3,                                                   -- Approved
    @ManagerUserId, DATEADD(DAY, -15, CAST(r.StartDate AS datetime2(0))),
    @HrUserId,      DATEADD(DAY, -12, CAST(r.StartDate AS datetime2(0))),
    DATEADD(DAY, -20, CAST(r.StartDate AS datetime2(0)))
FROM Ranged r
CROSS APPLY
(
    -- İş günü sayımı: aralıktaki her günü üretip hafta sonlarını eler.
    -- sys.all_objects yalnızca satır kaynağı olarak kullanılır (birkaç bin satır);
    -- izin süreleri tek haneli olduğu için fazlasıyla yeterli.
    SELECT COUNT(*) AS WorkingDays
    FROM
    (
        SELECT TOP (DATEDIFF(DAY, r.StartDate, r.EndDate) + 1)
               DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, r.StartDate) AS D
        FROM sys.all_objects
    ) g
    WHERE (DATEDIFF(DAY, '19000101', g.D) % 7) < 5      -- 0..4 = Pzt..Cum
) wd
-- İşe giriş tarihinden ÖNCEYE izin yazılmaz: ~15 aylık pencere yeni işe
-- girenlerde geçmişe taşar ve o kayıtlar mantıksız olurdu. Yeni başlayanlar
-- bu yüzden daha az kayıt alır — beklenen davranış.
WHERE r.StartDate >= (SELECT e.HireDate FROM dbo.Employees e WHERE e.Id = r.EmployeeId)
  AND wd.WorkingDays >= 1;

PRINT CONCAT('Eklendi: ', @@ROWCOUNT, ' onayli izin kaydi.');
GO


/* =============================================================================
   3) DOĞRULAMA

   Seed'in kendi kuralını bozmadığını gösterir. Beklenen: aşım listesi BOŞ.
   ============================================================================= */

PRINT '';
PRINT '--- Departman bazinda dagilim ---';

SELECT
    d.Name                                                   AS Departman,
    COUNT(*)                                                 AS IzinSayisi,
    SUM(CASE WHEN l.Type = 1 THEN 1 ELSE 0 END)              AS Yillik,
    SUM(CASE WHEN l.Type = 2 THEN 1 ELSE 0 END)              AS Ucretsiz,
    SUM(CASE WHEN l.Type = 3 THEN 1 ELSE 0 END)              AS Hastalik,
    SUM(l.WorkingDays)                                       AS ToplamIsGunu
FROM dbo.LeaveRequests l
JOIN dbo.Employees   e ON e.Id = l.EmployeeId
JOIN dbo.Departments d ON d.Id = e.DepartmentId
WHERE l.Description LIKE '[[]seed]%'
GROUP BY d.Name
ORDER BY COUNT(*) DESC;

PRINT '--- Yillik izin hakkini asan calisan (BOS OLMALI) ---';

SELECT
    e.Id,
    e.FirstName + ' ' + e.LastName                           AS Calisan,
    ISNULL(e.AnnualLeaveDays, 14)                            AS Hak,
    SUM(l.WorkingDays)                                       AS KullanilanYillik
FROM dbo.LeaveRequests l
JOIN dbo.Employees e ON e.Id = l.EmployeeId
WHERE l.Type = 1
  AND l.Status IN (1, 2, 3)
GROUP BY e.Id, e.FirstName, e.LastName, e.AnnualLeaveDays
HAVING SUM(l.WorkingDays) > ISNULL(e.AnnualLeaveDays, 14);

PRINT '--- Ayni calisanda cakisan izin (BOS OLMALI) ---';

-- Uygulama tarih çakışmasını reddeder; seed de bu kurala uymalı.
-- a.Id < b.Id: her çifti bir kez listele (aksi halde her çakışma iki satır olurdu).
SELECT TOP 20
    a.EmployeeId,
    a.Id AS Izin1, a.StartDate AS Baslangic1, a.EndDate AS Bitis1,
    b.Id AS Izin2, b.StartDate AS Baslangic2, b.EndDate AS Bitis2
FROM dbo.LeaveRequests a
JOIN dbo.LeaveRequests b
  ON b.EmployeeId = a.EmployeeId
 AND b.Id > a.Id
 AND a.StartDate <= b.EndDate
 AND b.StartDate <= a.EndDate
WHERE a.Status <> 4 AND b.Status <> 4;      -- reddedilenler yer tutmaz

PRINT '--- Hafta sonuna denk gelen kayit (BOS OLMALI) ---';

SELECT TOP 20 Id, StartDate, EndDate, WorkingDays
FROM dbo.LeaveRequests
WHERE Description LIKE '[[]seed]%'
  AND ((DATEDIFF(DAY, '19000101', StartDate) % 7) >= 5
       OR (DATEDIFF(DAY, '19000101', EndDate) % 7) >= 5);
GO

/* --- Onay izi uyarısı ------------------------------------------------------ */
IF EXISTS
(
    SELECT 1 FROM dbo.LeaveRequests
    WHERE Description LIKE '[[]seed]%'
      AND ManagerApprovedByUserId = HrApprovedByUserId
)
    PRINT 'UYARI: Iki onay asamasi ayni hesaba yazildi (sistemde ayri Manager/HR hesabi yok). Uygulama kurali bunu normalde engeller; seed veri icin zararsizdir.';
GO

PRINT '';
PRINT 'OK: Izin gecmisi seed i tamamlandi.';
GO

-- Ön koşul kontrolü NOEXEC ON bırakmış olabilir; oturumu temiz devret
-- (12_seed_org_kadro.sql ile aynı kalıp).
SET NOEXEC OFF;
GO
