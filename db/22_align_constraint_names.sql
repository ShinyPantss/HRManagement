/* =============================================================================
   HRManagement — Kısıt adlarını hizalama                            (2026-08-11)

   NEDEN GEREKLİ
   Veritabanı ilk olarak 01_schema.sql ile kurulmuştu ve o dosya PRIMARY KEY /
   FOREIGN KEY kısıtlarına isim VERMİYORDU. SQL Server o durumda kendi adını
   üretir ve sonuna bir hash ekler:

       PK__Departme__3214EC071C2EC5C5      FK__Employees__Depar__5070F446

   05_full_setup.sql sıfırdan kurulumda bu kısıtları açık adlarla oluşturuyor,
   ama mevcut veritabanı zaten var olduğu için o CREATE TABLE bloğu atlandı —
   dolayısıyla adlar otomatik hâlinde kaldı.

   Çalışma anında bunun bir etkisi yok: ne Dapper ne EF Core kısıt adı kullanır.
   Sorun İLERİDE çıkar: EF Core migration'ları kısıtları ADIYLA düşürüp yeniden
   kurar. Model "FK_Employees_Departments" derken veritabanında o adda bir şey
   yoksa migration çalışma anında patlar. Ayrıca hash'li adlar her ortamda
   FARKLI olduğu için hangi kısıtın ne olduğu okunmaz hâle gelir.

   NE YAPAR
   Yalnızca ADLANDIRMA değiştirir — sp_rename metadata'ya dokunur. Hiçbir kısıt
   düşürülmez, yeniden kurulmaz; veri, index sayfaları ve doğrulama davranışı
   aynen kalır. Tabloya kilit alınmaz denemez, ama işlem milisaniyeliktir.

   Tekrar çalıştırılabilir (idempotent): adı zaten doğru olanlara dokunmaz.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE HRManagementDb;
GO

/* =============================================================================
   1) PRIMARY KEY'LER  →  PK_<TabloAdı>

   Hedef ad mekanik olarak türetilebildiği için dinamik üretiliyor; hash'li
   adları buraya elle yazmak mümkün değil, çünkü her veritabanında farklılar.
   ============================================================================= */
DECLARE @pkSql nvarchar(max) = N'';
DECLARE @pkCount int = 0;

SELECT @pkSql += N'EXEC sp_rename N''dbo.' + kc.name + N''', N''PK_' + t.name + N''', ''OBJECT'';' + CHAR(13) + CHAR(10),
       @pkCount += 1
FROM sys.key_constraints kc
JOIN sys.tables t ON t.object_id = kc.parent_object_id
WHERE kc.type = 'PK'
  AND t.is_ms_shipped = 0
  AND t.name <> 'sysdiagrams'
  AND kc.name <> N'PK_' + t.name;

IF @pkCount > 0
BEGIN
    EXEC sys.sp_executesql @pkSql;
    PRINT CONCAT('22: ', @pkCount, ' primary key adi hizalandi.');
END
ELSE
    PRINT '22: primary key adlari zaten dogru, atlandi.';
GO


/* =============================================================================
   2) FOREIGN KEY'LER

   Hedef adlar mekanik DEĞİL — aynı tabloya birden çok FK gidebiliyor
   (LeaveRequests'ten Users'a üç ayrı onay/ret sütunu gibi) ve isim o sütunun
   ANLAMINI taşıyor. Bu yüzden eşleme açıkça yazılıyor. Eşleme, koddaki Fluent
   konfigürasyonlardaki HasConstraintName(...) çağrılarının birebir aynısıdır.
   ============================================================================= */
DECLARE @hedef TABLE (TabloAdi sysname, SutunAdi sysname, IstenenAd sysname);

INSERT INTO @hedef (TabloAdi, SutunAdi, IstenenAd) VALUES
    ('Units',           'DepartmentId',            'FK_Units_Departments'),

    ('Employees',       'DepartmentId',            'FK_Employees_Departments'),
    ('Employees',       'UnitId',                  'FK_Employees_Units'),
    ('Employees',       'UserId',                  'FK_Employees_Users'),
    ('Employees',       'ManagerId',               'FK_Employees_Manager'),

    ('Interns',         'DepartmentId',            'FK_Interns_Departments'),
    ('Interns',         'UnitId',                  'FK_Interns_Units'),
    ('Interns',         'MentorId',                'FK_Interns_Employees'),
    ('Interns',         'UserId',                  'FK_Interns_Users'),

    ('LeaveRequests',   'EmployeeId',              'FK_LeaveRequests_Employees'),
    ('LeaveRequests',   'InternId',                'FK_LeaveRequests_Interns'),
    ('LeaveRequests',   'ManagerApprovedByUserId', 'FK_LeaveRequests_ManagerApprovedBy'),
    ('LeaveRequests',   'HrApprovedByUserId',      'FK_LeaveRequests_HrApprovedBy'),
    ('LeaveRequests',   'RejectedByUserId',        'FK_LeaveRequests_RejectedBy'),

    ('EmployeeNotes',   'EmployeeId',              'FK_EmployeeNotes_Employees'),
    ('EmployeeNotes',   'AuthorUserId',            'FK_EmployeeNotes_Users'),

    ('InternNotes',     'InternId',                'FK_InternNotes_Interns'),
    ('InternNotes',     'AuthorUserId',            'FK_InternNotes_Users'),

    ('InternTasks',     'InternId',                'FK_InternTasks_Interns'),
    ('InternTasks',     'CreatedByUserId',         'FK_InternTasks_Users'),

    ('AccountRequests', 'EmployeeId',              'FK_AccountRequests_Employees'),
    ('AccountRequests', 'InternId',                'FK_AccountRequests_Interns'),
    ('AccountRequests', 'RequestedByUserId',       'FK_AccountRequests_RequestedBy'),
    ('AccountRequests', 'ReviewedByUserId',        'FK_AccountRequests_ReviewedBy');

/* Kısıtın kendisi ADIYLA değil, DURDUĞU SÜTUNLA bulunuyor: mevcut adı zaten
   bilmiyoruz (hash'li). Projedeki tüm FK'lar tek sütunlu olduğu için bu eşleme
   tekildir. */
DECLARE @fkSql nvarchar(max) = N'';
DECLARE @fkCount int = 0;

SELECT @fkSql += N'EXEC sp_rename N''dbo.' + fk.name + N''', N''' + h.IstenenAd + N''', ''OBJECT'';' + CHAR(13) + CHAR(10),
       @fkCount += 1
FROM @hedef h
JOIN sys.foreign_keys fk
     ON OBJECT_NAME(fk.parent_object_id) = h.TabloAdi
JOIN sys.foreign_key_columns fkc
     ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c
     ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE c.name = h.SutunAdi
  AND fk.name <> h.IstenenAd;

IF @fkCount > 0
BEGIN
    EXEC sys.sp_executesql @fkSql;
    PRINT CONCAT('22: ', @fkCount, ' foreign key adi hizalandi.');
END
ELSE
    PRINT '22: foreign key adlari zaten dogru, atlandi.';
GO


/* =============================================================================
   3) DOĞRULAMA — geriye hash'li ad kalmamalı
   ============================================================================= */
DECLARE @kalan int =
(
    SELECT COUNT(*)
    FROM sys.objects
    WHERE type IN ('PK', 'F')
      AND is_ms_shipped = 0
      AND name LIKE '%\_\_%' ESCAPE '\'
      AND OBJECT_NAME(parent_object_id) <> 'sysdiagrams'
);

IF @kalan > 0
BEGIN
    PRINT CONCAT('UYARI: hala otomatik adli ', @kalan, ' kisit var. Liste:');
    SELECT name AS KisitAdi, type_desc AS Tur, OBJECT_NAME(parent_object_id) AS Tablo
    FROM sys.objects
    WHERE type IN ('PK', 'F') AND is_ms_shipped = 0
      AND name LIKE '%\_\_%' ESCAPE '\'
      AND OBJECT_NAME(parent_object_id) <> 'sysdiagrams';
END
ELSE
    PRINT 'OK: tum PK/FK adlari acik ve model ile ayni.';
GO
