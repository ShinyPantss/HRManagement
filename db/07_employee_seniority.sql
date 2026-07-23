/* =============================================================================
   HRManagement — Çalışan kıdem/ünvan seviyesi   (2026-07-23)

   Employees'e Seniority sütununu ekler. Sıfırdan kurulumda 05_full_setup.sql
   zaten içerir; bu dosya mevcut veritabanı için DELTA'dır. Tekrar çalıştırılabilir.

   Seniority (Domain/Enums/SeniorityLevel.cs):
     1=GenelMudur 2=GenelMudurYardimcisi 3=Mudur 4=MudurYardimcisi
     5=KidemliUzman 6=Uzman
   Mevcut kayıtlar için null olabilir (kıdem sonradan girilir).
   Stajyerler bu sütuna sahip değildir (ayrı tablo).
   ============================================================================= */

USE HRManagementDb;
GO

IF COL_LENGTH('dbo.Employees', 'Seniority') IS NULL
    ALTER TABLE dbo.Employees ADD Seniority int NULL;
GO

/* Geçersiz sayı (ör. 99) girilmesin — enum aralığını DB de korur. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_Seniority')
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_Seniority
        CHECK (Seniority IS NULL OR Seniority BETWEEN 1 AND 6);
GO

PRINT 'OK: Employees.Seniority hazir.';
GO
