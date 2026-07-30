/* =============================================================================
   HRManagement — İK panosu stored procedure'ü                      (2026-07-29)

   NE YAPAR
     Ana sayfa panosunun TÜM verisini tek çağrıda, BEŞ result set hâlinde döner.
     Önceki hâlinde handler dört repository'yi ayrı ayrı çağırıp Employees,
     Interns ve LeaveRequests tablolarının TAMAMINI belleğe çekiyor, sayımı
     LINQ ile yapıyordu. Buradaki kazanç round-trip'ten çok OVERFETCH'te:
     sayılacak şey artık ağdan geçmiyor, sayı geçiyor.

   ÇALIŞTIRMA SIRASI: şema script'lerinden sonra, herhangi bir zamanda.
     CREATE OR ALTER olduğu için tekrar tekrar çalıştırılabilir.

   RESULT SET SIRASI — DEĞİŞTİRİLEMEZ
     İstemci (DashboardRepository) sonuçları bu sırayla okur. Sıra kayarsa
     derleyici uyarmaz; ekranda sessizce yanlış veri görünür.
       1) Özet         — tek satır, tüm skaler metrikler
       2) Kıdem        — kademe başına aktif çalışan
       3) Şu an izinde — bugünü kapsayan onaylı izinler
       4) Yaklaşan     — pencere içinde başlayacak onaylı izinler
       5) Trend        — son N ayın izin kullanımı

   BİLİNÇLİ KARARLAR
     1) ENUM DEĞERLERİ PARAMETRE. Status/Gender sayıları SQL'e gömülmez,
        C#'taki enum'dan geçirilir. Aksi hâlde enum değeri değiştiğinde kod
        derlenir, testler geçer, SP sessizce yanlış sayar — bulunması en zor
        hata türü.

     2) EŞİKLER DE PARAMETRE. "5 gündür bekliyor", "14 gün içinde" gibi
        değerler ekrandaki metinleri de yazıyor; tek kaynak Application'da.

     3) @Today PARAMETRE, GETDATE() DEĞİL. Uygulama "bugün"ü UTC olarak
        tanımlıyor (LeaveEntitlement ile aynı kaynak). SP kendi saatine
        bakarsa sistemde iki farklı "bugün" doğar.

     4) METİN ÜRETİLMEZ. Tür ve kişi tipi için LeaveTypeId (int) ve IsIntern
        (bit) döner; "Yıllık İzin" / "Stajyer" metinleri C# tarafında üretilir.
        (1) numaralı kararın simetriği: enum bilgisi tek yönde akar.

     5) LİSTELER KESİLMEZ. Ekran ilk 6 satırı gösterip "N kişi daha" yazıyor;
        bu sayıyı verebilmek için tam liste gerekiyor. TOP ile kesmek, ayrıca
        bir COUNT sorgusu daha gerektirirdi.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE HRManagementDb;
GO

CREATE OR ALTER PROCEDURE dbo.usp_HrDashboard_Get
    @Today                   date,
    -- Enum karşılıkları (C#'taki LeaveStatus / Gender)
    @StatusPending           int,
    @StatusPendingHr         int,
    @StatusApproved          int,
    @GenderMale              int,
    @GenderFemale            int,
    -- Eşikler (C#'taki sabitler)
    @OverdueDays             int,
    @UpcomingWindowDays      int,
    @InternEndingWindowDays  int,
    @TrendMonths             int
AS
BEGIN
    -- Dapper'ın "N rows affected" mesajını result set sanmaması için şart.
    SET NOCOUNT ON;

    /* ── 1) ÖZET ───────────────────────────────────────────────────────────
       Tek satır, tüm skaler metrikler. Alt sorgular tablo başına tek tarama
       yapar; bunları ayrı SELECT'lere bölmek dört ekstra result set demekti. */
    SELECT
        -- Kadro
        (SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1)              AS TotalActiveEmployees,
        (SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1 AND UserId IS NULL)
                                                                             AS EmployeesWithoutAccount,
        (SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1 AND Gender = @GenderMale)
                                                                             AS MaleCount,
        (SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1 AND Gender = @GenderFemale)
                                                                             AS FemaleCount,
        (SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1 AND Gender IS NULL)
                                                                             AS GenderUnspecifiedCount,

        -- Stajyer
        (SELECT COUNT(*) FROM dbo.Interns WHERE EndDate >= @Today)           AS ActiveInterns,
        (SELECT COUNT(*) FROM dbo.Interns
          WHERE EndDate >= @Today
            AND EndDate <= DATEADD(DAY, @InternEndingWindowDays, @Today))    AS InternsEndingSoon,

        -- İzin
        (SELECT COUNT(*) FROM dbo.LeaveRequests
          WHERE Status = @StatusApproved
            AND StartDate <= @Today AND EndDate >= @Today)                   AS OnLeaveNowCount,
        (SELECT COUNT(*) FROM dbo.LeaveRequests
          WHERE Status IN (@StatusPending, @StatusPendingHr))                AS PendingLeaveRequests,

        -- Yaşlandırma: soru "kaç talep var" değil, "ne kadardır bekliyor".
        (SELECT COUNT(*) FROM dbo.LeaveRequests
          WHERE Status IN (@StatusPending, @StatusPendingHr)
            AND DATEDIFF(DAY, CAST(CreatedAt AS date), @Today) >= @OverdueDays)
                                                                             AS OverduePendingCount,
        -- Bekleyen yoksa MAX null döner; ekran 0 bekliyor.
        ISNULL((SELECT MAX(DATEDIFF(DAY, CAST(CreatedAt AS date), @Today))
                FROM dbo.LeaveRequests
                WHERE Status IN (@StatusPending, @StatusPendingHr)), 0)      AS OldestPendingDays;


    /* ── 2) KIDEM DAĞILIMI ─────────────────────────────────────────────────
       Kıdemi girilmemiş kayıtlar (NULL) ayrı bir satır olarak döner ve en
       sona sıralanır — ekranda "Belirtilmemiş" olarak gösterilir. */
    SELECT
        Seniority,
        COUNT(*) AS [Count]
    FROM dbo.Employees
    WHERE IsActive = 1
    GROUP BY Seniority
    ORDER BY ISNULL(Seniority, 2147483647);   -- NULL en sona


    /* ── 3) ŞU AN İZİNDE ───────────────────────────────────────────────────
       Bugünü kapsayan onaylı izinler. Çalışan ve stajyer birlikte; hangisi
       olduğu IsIntern ile bildirilir, metni C# üretir. */
    SELECT
        COALESCE(e.FirstName + ' ' + e.LastName, i.FirstName + ' ' + i.LastName) AS SubjectName,
        CAST(CASE WHEN lr.InternId IS NOT NULL THEN 1 ELSE 0 END AS bit)         AS IsIntern,
        lr.Type                                                                  AS LeaveTypeId,
        lr.StartDate,
        lr.EndDate
    FROM dbo.LeaveRequests lr
    LEFT JOIN dbo.Employees e ON e.Id = lr.EmployeeId
    LEFT JOIN dbo.Interns   i ON i.Id = lr.InternId
    WHERE lr.Status = @StatusApproved
      AND lr.StartDate <= @Today
      AND lr.EndDate   >= @Today
    ORDER BY lr.EndDate;                       -- önce dönecek olan üstte


    /* ── 4) YAKLAŞAN İZİNLER ───────────────────────────────────────────────
       Pencere içinde BAŞLAYACAK onaylı izinler. Bugün başlayanlar 3. sette
       zaten var, bu yüzden StartDate > @Today. */
    SELECT
        COALESCE(e.FirstName + ' ' + e.LastName, i.FirstName + ' ' + i.LastName) AS SubjectName,
        CAST(CASE WHEN lr.InternId IS NOT NULL THEN 1 ELSE 0 END AS bit)         AS IsIntern,
        lr.Type                                                                  AS LeaveTypeId,
        lr.StartDate,
        lr.EndDate,
        lr.WorkingDays,
        DATEDIFF(DAY, @Today, lr.StartDate)                                      AS DaysUntilStart
    FROM dbo.LeaveRequests lr
    LEFT JOIN dbo.Employees e ON e.Id = lr.EmployeeId
    LEFT JOIN dbo.Interns   i ON i.Id = lr.InternId
    WHERE lr.Status = @StatusApproved
      AND lr.StartDate >  @Today
      AND lr.StartDate <= DATEADD(DAY, @UpcomingWindowDays, @Today)
    ORDER BY lr.StartDate;                     -- en yakın başlayan üstte


    /* ── 5) AYLIK TREND ────────────────────────────────────────────────────
       Aylar TAKVİMDEN üretilir, veriden değil: hiç izin olmayan ay da sıfır
       değerle görünmeli. Sadece GROUP BY yazsaydık boş aylar listeden düşer
       ve trend olduğundan düzgün görünürdü.

       sys.all_objects yalnızca satır kaynağı; @TrendMonths tek haneli olduğu
       için fazlasıyla yeterli (16_seed_leave_history.sql ile aynı kalıp). */
    ;WITH MonthList AS
    (
        SELECT TOP (@TrendMonths)
               DATEADD(MONTH,
                       -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1),
                       DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1)) AS MonthStart
        FROM sys.all_objects
    )
    SELECT
        YEAR(m.MonthStart)                          AS [Year],
        MONTH(m.MonthStart)                         AS [Month],
        ISNULL(SUM(lr.WorkingDays), 0)              AS WorkingDays,
        COUNT(lr.Id)                                AS RequestCount
    FROM MonthList m
    -- LEFT JOIN şart: eşleşme yoksa ay yine de satır olarak kalmalı.
    LEFT JOIN dbo.LeaveRequests lr
           ON lr.Status = @StatusApproved
          AND lr.StartDate >= m.MonthStart
          AND lr.StartDate <  DATEADD(MONTH, 1, m.MonthStart)
    GROUP BY m.MonthStart
    ORDER BY m.MonthStart;                     -- eskiden yeniye
END
GO

PRINT 'OK: dbo.usp_HrDashboard_Get olusturuldu/guncellendi.';
GO
