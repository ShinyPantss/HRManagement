/* =============================================================================
   HRManagement — mevcut veritabanları için düzeltmeler   (2026-07-22)

   01_schema.sql artık doğru hâli üretiyor; bu dosya DAHA ÖNCE oluşturulmuş
   bir veritabanını aynı noktaya taşımak içindir. Sıfırdan kurulumda gerekmez.

   Her adım hem "zaten yapılmış mı" hem "yapmaya engel veri var mı" diye
   bakar. Engel varsa sessizce geçmez, hata verip durumu bildirir — çünkü
   kalan tek çözüm veriyi uydurmak olurdu ve o daha kötüdür.
   ============================================================================= */

USE HRManagementDb;
GO


/* --- 1) Employees.DateOfBirth → NOT NULL ------------------------------------
   Sorun: sütun NULL kabul ediyordu, ama Domain/Entities/Employee.cs içinde
   DateOfBirth nullable DEĞİL (DateTime). Tek bir NULL satır Dapper'ın map
   işlemini bozar ve yalnızca o satırı değil TÜM çalışan listesini 500 yapar.  */
IF EXISTS (SELECT 1 FROM dbo.Employees WHERE DateOfBirth IS NULL)
BEGIN
    RAISERROR('ATLANDI: Employees.DateOfBirth sutununda NULL kayitlar var. Once doldurun: SELECT Id, FirstName, LastName FROM dbo.Employees WHERE DateOfBirth IS NULL;', 16, 1);
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Employees')
                  AND name = 'DateOfBirth'
                  AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.Employees ALTER COLUMN DateOfBirth date NOT NULL;
    PRINT 'OK: Employees.DateOfBirth -> NOT NULL';
END
ELSE
    PRINT 'ATLANDI: Employees.DateOfBirth zaten NOT NULL';
GO


/* --- 2) Employees.Email → UNIQUE --------------------------------------------
   Sorun: benzersizliği yalnızca uygulama kontrol ediyordu. Uygulama dışından
   girilen kayıt kuralı deler; ayrıca "önce SELECT sonra INSERT" kontrolü
   eşzamanlı iki istek arasından sıyrılabilir. Asıl garantiyi kısıt verir.
   (Users tablosunda bu doğru yapılmış, Employees'te eksik kalmış.)            */
IF EXISTS (SELECT Email FROM dbo.Employees GROUP BY Email HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('ATLANDI: Employees.Email sutununda tekrar eden degerler var. Kontrol: SELECT Email, COUNT(*) FROM dbo.Employees GROUP BY Email HAVING COUNT(*) > 1;', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_Employees_Email')
BEGIN
    ALTER TABLE dbo.Employees ADD CONSTRAINT UQ_Employees_Email UNIQUE (Email);
    PRINT 'OK: UQ_Employees_Email eklendi';
END
ELSE
    PRINT 'ATLANDI: UQ_Employees_Email zaten var';
GO


/* --- 3) Eksik index'ler ------------------------------------------------------
   FK sütunlarına index SQL Server tarafından otomatik kurulmaz.
   01_schema.sql bunları zaten IF NOT EXISTS ile ekliyor; oradan tekrar
   çalıştırmak yerine burada da güvenle uygulanabilir.                          */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId')
    CREATE INDEX IX_Employees_DepartmentId ON dbo.Employees (DepartmentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_UserId')
    CREATE INDEX IX_Employees_UserId ON dbo.Employees (UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Interns_DepartmentId')
    CREATE INDEX IX_Interns_DepartmentId ON dbo.Interns (DepartmentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Interns_MentorId')
    CREATE INDEX IX_Interns_MentorId ON dbo.Interns (MentorId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_EmployeeId')
    CREATE INDEX IX_LeaveRequests_EmployeeId ON dbo.LeaveRequests (EmployeeId);
GO

PRINT 'Duzeltmeler tamamlandi.';
GO
