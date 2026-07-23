/* =============================================================================
   HRManagement — Employees.Position sütununu kaldır   (2026-07-23)

   Pozisyon artık AYRI bir alan değil; gösterimde Departman + Kıdem (Seniority)
   birleşiminden türetilir ("IT Uzmanı"). Serbest metin sütununa gerek kalmadı.

   Mevcut veritabanı için DELTA. Sıfırdan kurulumda 05_full_setup.sql zaten
   Position'sız oluşturur. Tekrar çalıştırılabilir.

   NOT: 07_employee_seniority.sql ÖNCE çalıştırılmış olmalı (kıdem sütunu bu
   sütunun yerini alıyor).
   ============================================================================= */

USE HRManagementDb;
GO

IF COL_LENGTH('dbo.Employees', 'Position') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Employees DROP COLUMN Position;
    PRINT 'OK: Employees.Position kaldirildi (pozisyon artik Departman+Kidem turetiliyor)';
END
ELSE
    PRINT 'ATLANDI: Employees.Position zaten yok';
GO
