/* =============================================================================
   HRManagement — İzin kuralları: iş günü + hastalık raporu   (2026-07-23)

   LeaveRequests'e iki sütun ekler:
     WorkingDays   → talebin iş günü sayısı (hafta sonu hariç); oluşturulurken
                     C# hesaplar ve saklar. Bakiye toplamı bu sütunu SUM'lar.
     MedicalReport → hastalık izninde zorunlu rapor bilgisi (metin).

   Mevcut veritabanı için DELTA. Sıfırdan kurulumda 05_full_setup.sql zaten içerir.
   Tekrar çalıştırılabilir.
   ============================================================================= */

USE HRManagementDb;
GO

IF COL_LENGTH('dbo.LeaveRequests', 'WorkingDays') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD WorkingDays int NOT NULL CONSTRAINT DF_LeaveRequests_WorkingDays DEFAULT (0);
    -- Mevcut (test) satırları için kaba backfill: takvim günü. Yeni satırlar
    -- C#'tan gerçek iş günüyle gelir. (Test verisi olduğu için yaklaşık yeterli.)
    EXEC('UPDATE dbo.LeaveRequests SET WorkingDays = DATEDIFF(DAY, StartDate, EndDate) + 1 WHERE WorkingDays = 0');
    PRINT 'OK: LeaveRequests.WorkingDays eklendi';
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'MedicalReport') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD MedicalReport nvarchar(500) NULL;
GO

PRINT 'OK: Izin kurallari semasi hazir.';
GO
