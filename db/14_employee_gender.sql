/* =============================================================================
   HRManagement — Employees.Gender (Cinsiyet)                        (2026-07-27)

   Erkek = 1, Kadın = 2. Kolon NULLABLE'dır: eski kayıtlar boş kalabilir; alanın
   ZORUNLULUĞU uygulama katmanında (Create/Update validator) uygulanır — böylece
   mevcut seed/kayıtlar bozulmadan, yeni kayıtlar dolu gelir. Seniority ile aynı
   sözleşme (int + CK aralığı). Idempotent — tekrar çalıştırılabilir.

   Sıfırdan kurulumda 05_full_setup.sql da bu kolonu içerir.
   ============================================================================= */

USE HRManagementDb;
GO

IF COL_LENGTH('dbo.Employees', 'Gender') IS NULL
    ALTER TABLE dbo.Employees ADD Gender int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_Gender')
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_Gender
        CHECK (Gender IS NULL OR Gender BETWEEN 1 AND 2);
GO

PRINT 'OK: Employees.Gender eklendi (Erkek=1, Kadin=2).';
GO
