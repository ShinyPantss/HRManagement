/* ============================================================================
   18 — Bilgi Teknolojileri / "Dijital Platformlar ve Entegrasyon" biriminin
        yönetici hattını departmanın geri kalanıyla aynı kalıba getirir.

   NE OLDU
   17 numaralı betik GMY'leri güncel GM'e bağladı ama Genel Müdür değişiminin
   ikinci bir kalıntısı daha vardı: eski GM'in kendi birimi (Dijital Platformlar)
   toptan GM'e bağlı kalmıştı.

   MEVCUT (bozuk)                         HEDEF (departmanın geri kalanıyla aynı)
     Ömer Yalçın (GM)                       Ömer Yalçın (GM)
       ├─ Mücahit CAN      (Müdür)            └─ Pınar Yıldırım (GMY)
       ├─ Nazlı Aydın      (Müdür Yrd.)            └─ Mücahit CAN   (Müdür)
       ├─ Mehmet Polat     (Kıd. Uzman)                 ├─ Nazlı Aydın
       └─ Zeynep Doğan     (Uzman)                      ├─ Mehmet Polat
                                                        └─ Zeynep Doğan

   Karşılaştırma — BT'nin diğer 6 birimi zaten böyle:
     Sistem ve Network → Aytaç Kaçak (Müdür) → Pınar Yıldırım (GMY)
       ve birim üyeleri (Müdür Yrd. / Kıd. Uzman / Uzman) → Aytaç Kaçak

   Sonuç: Genel Müdür'e YALNIZCA GMY'ler bağlı kalır; her birim müdürü kendi
   birimini yönetir. Mücahit CAN'ın detay ekranında "Doğrudan ekibi" bölümü de
   bu yüzden boştu — 17'den sonra hiç astı kalmamıştı.

   Betik TEKRAR ÇALIŞTIRILABİLİR: yalnızca hedeflenen bağ yanlışsa dokunur.
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @Gm         int = 26;   -- Ömer Yalçın      · Genel Müdür
DECLARE @BtGmy      int = 211;  -- Pınar Yıldırım   · BT Genel Müdür Yardımcısı
DECLARE @BirimMudur int = 24;   -- Mücahit CAN      · Dijital Platformlar müdürü

DECLARE @BirimId int = (SELECT UnitId FROM dbo.Employees WHERE Id = @BirimMudur);

/* Güvenlik: hedef zincir gerçekten beklediğimiz kademelerde mi?
   Değilse hiç dokunma — yanlış varsayımla toplu bağlama, düzeltmeye
   çalıştığımız hatanın aynısı olur. */
IF @BirimId IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @Gm         AND Seniority = 1 AND IsActive = 1)
   OR NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @BtGmy      AND Seniority = 2 AND IsActive = 1)
   OR NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @BirimMudur AND Seniority = 3 AND IsActive = 1)
BEGIN
    RAISERROR('Beklenen kademe yapisi bulunamadi (GM/GMY/Mudur veya birim). Betik durduruldu.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

    /* 1) Birim müdürü departmanın GMY'sine bağlanır — diğer birimlerle aynı. */
    UPDATE dbo.Employees
    SET ManagerId = @BtGmy, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @BirimMudur AND ManagerId <> @BtGmy;

    PRINT CONCAT('Birim muduru guncellendi: ', @@ROWCOUNT);

    /* 2) Birimin geri kalanı kendi müdürüne bağlanır.
          Müdürün kendisi hariç; yalnızca ondan DÜŞÜK kıdemdekiler. */
    UPDATE e
    SET ManagerId = @BirimMudur, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Employees e
    WHERE e.UnitId = @BirimId
      AND e.Id <> @BirimMudur
      AND e.IsActive = 1
      AND e.Seniority > 3            -- Müdür Yrd. ve altı
      AND e.ManagerId <> @BirimMudur;

    PRINT CONCAT('Birim uyesi guncellendi: ', @@ROWCOUNT);

COMMIT TRANSACTION;

/* Doğrulama 1: Genel Müdür'e artık yalnızca GMY'ler bağlı olmalı (0 satır dönmeli). */
SELECT e.Id, e.FirstName + ' ' + e.LastName AS [GM'e bagli ama GMY degil], e.Seniority
FROM dbo.Employees e
WHERE e.ManagerId = @Gm AND e.IsActive = 1 AND e.Seniority <> 2;
