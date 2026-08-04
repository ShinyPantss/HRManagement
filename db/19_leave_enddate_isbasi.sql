/* ============================================================================
   19 — İzin bitiş tarihinin ANLAMI değişti: "son izinli gün" → "işe başlama günü".

   NEDEN
   Kullanıcı kararı: talep formunda bitiş tarihi kişinin işe döneceği gündür ve
   izne SAYILMAZ (yarı açık aralık [Start, End)). Örn. 3'ü → 5'i = 2 gün izin.
   Kod tarafı buna göre güncellendi: LeaveEntitlement.WorkingDays, çakışma
   sorgusu (HasOverlapAsync), "şu an izinde" sorguları (18_sp_hr_dashboard) ve
   WebUI takvimi artık EndDate'i HARİÇ sayar.

   BU BETİK NE YAPAR
   Eski kayıtlar "son izinli gün" anlamıyla girilmişti. Anlam değişince aynı
   sayı bir gün eksik izin ifade eder olurdu. Fiili izin günleri değişmesin
   diye TÜM mevcut kayıtların EndDate'i +1 gün kaydırılır.
   WorkingDays'e DOKUNULMAZ: o değer eski anlamla (uçlar dahil) hesaplanmıştı;
   kaydırma sonrası yeni anlamla (bitiş hariç) aynı gün kümesini verir.

   TEKRAR ÇALIŞTIRILAMAZ BİR VERİ DÖNÜŞÜMÜDÜR — ikinci çalıştırma izinleri
   birer gün uzatırdı. Bu yüzden uygulandığı, veritabanı üzerine extended
   property olarak damgalanır ve betik ikinci seferde kendini atlar.
   ============================================================================ */

SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.extended_properties
           WHERE class = 0 AND name = N'Migration_19_LeaveEndDateIsbasi')
BEGIN
    PRINT '19: daha once uygulanmis, atlandi.';
    RETURN;
END

BEGIN TRANSACTION;

    UPDATE dbo.LeaveRequests
    SET EndDate   = DATEADD(DAY, 1, EndDate),
        UpdatedAt = SYSUTCDATETIME();

    PRINT CONCAT('19: ', @@ROWCOUNT, ' izin kaydinin EndDate''i +1 gun kaydirildi.');

    /* Uygulandı damgası (class 0 = veritabanının kendisi). */
    EXEC sys.sp_addextendedproperty
         @name  = N'Migration_19_LeaveEndDateIsbasi',
         @value = N'EndDate artik ise baslama gunu (haric); mevcut kayitlar +1 gun kaydirildi.';

COMMIT TRANSACTION;
