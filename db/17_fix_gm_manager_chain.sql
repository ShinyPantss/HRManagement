/* ============================================================================
   17 — Genel Müdür değişiminden kalan bozuk yönetici bağlarını onarır.

   NE OLDU
   Genel Müdürlük Mücahit CAN'dan (Id 24) Ömer Yalçın'a (Id 26) geçti; eski GM
   Müdür kademesine (Seniority 3) düşürüldü. Ancak Seniority (kıdem) ile
   ManagerId (raporlama hattı) AYRI sütunlardır: kıdem güncellendi, ona bağlı
   7 GMY'nin ManagerId'si 24'te kaldı.

   Sonuç: "Müdür'e raporlayan GMY" — hem kıdem hem departman kuralını ihlal eden
   bir org. Bu yalnızca ekranda yanlış görünmez; izin onay zinciri de
   (LeaveApprovalGuard → IsInManagerChainAsync) bu geçersiz bağ üzerinden işler,
   yani GMY'lerin izinleri gerçek GM'e hiç uğramaz.

   ÇÖZÜM
   Yedi GMY'yi güncel GM'e bağla. GM "departman üstü"dür (SeniorityLevel yorumu),
   bu yüzden farklı departmanlarda olmaları kuralı bozmaz.

   TEKRARI ÖNLENDİ
   UpdateEmployeeCommandHandler artık kıdem/departman değişiminde ASTLARI
   denetliyor ve bağı geçersiz kılacak değişikliği reddediyor
   (ManagerAssignment.GetIneligibilityReason).

   Betik TEKRAR ÇALIŞTIRILABİLİR: yalnızca hâlâ 24'e bağlı olanlara dokunur.
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @EskiGm int = 24;   -- Mücahit CAN  (artık Müdür)
DECLARE @YeniGm int = 26;   -- Ömer Yalçın  (Genel Müdür)

/* Güvenlik kontrolü: hedef gerçekten GM mi? Değilse hiçbir şey yapma —
   yanlış kişiye toplu bağlama, düzeltmeye çalıştığımız hatanın aynısı olurdu. */
IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @YeniGm AND Seniority = 1 AND IsActive = 1)
BEGIN
    RAISERROR('Hedef kayit aktif Genel Mudur (Seniority=1) degil. Betik durduruldu.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

    /* Yalnızca eski GM'e bağlı GMY'ler (Seniority = 2). Alt kademeler
       (Müdür ve altı) eski GM'in kendi departmanında kalabilir; onlara
       dokunmuyoruz — kuralı ihlal etmiyorlar. */
    UPDATE dbo.Employees
    SET ManagerId = @YeniGm,
        UpdatedAt = SYSUTCDATETIME()
    WHERE ManagerId = @EskiGm
      AND Seniority = 2
      AND IsActive = 1;

    PRINT CONCAT('Guncellenen GMY sayisi: ', @@ROWCOUNT);

COMMIT TRANSACTION;

/* Doğrulama: kuralı ihlal eden bağ kalmamalı (0 satır dönmeli). */
SELECT e.Id, e.FirstName + ' ' + e.LastName AS Calisan, e.Seniority AS Kidem,
       m.FirstName + ' ' + m.LastName AS Yonetici, m.Seniority AS YonKidem
FROM dbo.Employees e
JOIN dbo.Employees m ON m.Id = e.ManagerId
WHERE e.IsActive = 1
  AND (m.Seniority IS NULL OR m.Seniority > 3                              -- yönetici kademesinde değil
       OR (e.Seniority IS NOT NULL AND m.Seniority >= e.Seniority)         -- kıdem yetersiz
       OR (m.Seniority <> 1 AND m.DepartmentId <> e.DepartmentId));        -- farklı departman (GM hariç)
