/* =============================================================================
   HRManagement — İK modülü genişletmesi   (2026-07-22)

   Gereksinim dokümanının §5.2, §5.3, §5.4 ve §5.5 maddelerinin veri modeli.
   Mevcut bir veritabanını yeni şemaya taşır. Sıfırdan kurulumda 01_schema.sql
   zaten bu hâli üretir; bu dosya yalnızca DELTA'dır.

   ÖNCE 03_fixes.sql çalıştırılmış olmalıdır.

   Bölümler:
     1) Audit sütunları (tüm tablolar)
     2) Employees.ManagerId — ekip/hiyerarşi kavramı
     3) LeaveRequests — stajyer talepleri + onay izi
     4) EmployeeNotes
     5) InternTasks
     6) InternNotes
     7) LeaveBalances
   ============================================================================= */

USE HRManagementDb;
GO


/* =============================================================================
   1) AUDIT SÜTUNLARI

   CreatedAt / UpdatedAt her ana tabloya eklenir.

   Neden UTC (SYSUTCDATETIME, GETDATE değil): sunucunun saat dilimi değişirse
   ya da uygulama başka bir bölgede çalışırsa yerel saat karşılaştırmaları
   bozulur. Kayıt UTC tutulur, kullanıcıya gösterilirken çevrilir.

   UpdatedAt NULL: "hiç güncellenmedi" ile "şu an güncellendi" farkını
   koruyoruz. CreatedAt ile aynı değere set etmek bu bilgiyi yok ederdi.
   ============================================================================= */

IF COL_LENGTH('dbo.Departments', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Departments ADD
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NULL;
GO

IF COL_LENGTH('dbo.Users', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Users ADD
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NULL;
GO

IF COL_LENGTH('dbo.Employees', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Employees ADD
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NULL;
GO

IF COL_LENGTH('dbo.Interns', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Interns ADD
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Interns_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NULL;
GO

IF COL_LENGTH('dbo.LeaveRequests', 'CreatedAt') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_LeaveRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NULL;
GO


/* =============================================================================
   2) Employees.ManagerId — EKİP KAVRAMI

   §5.5 "Yönetici Dashboard: kendi ekibindeki çalışan sayısı, ekibinden gelen
   bekleyen izin talepleri" — kimin kime bağlı olduğu bilgisi şemada hiç yoktu.

   Kendine referans veren FK: bir çalışanın yöneticisi yine bir çalışandır.
   NULL olabilir (en tepedeki kişinin yöneticisi yoktur).

   DİKKAT: SQL Server kendine referans veren FK'da ON DELETE CASCADE'e izin
   vermez (sonsuz döngü riski). NO ACTION kalır; yöneticisi silinmek istenen
   çalışanlar için kural Application katmanında ele alınacak.
   ============================================================================= */

IF COL_LENGTH('dbo.Employees', 'ManagerId') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD ManagerId int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Manager')
    ALTER TABLE dbo.Employees ADD CONSTRAINT FK_Employees_Manager
        FOREIGN KEY (ManagerId) REFERENCES dbo.Employees (Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_ManagerId')
    CREATE INDEX IX_Employees_ManagerId ON dbo.Employees (ManagerId);
GO


/* =============================================================================
   3) LeaveRequests — STAJYER TALEPLERİ + ONAY İZİ

   §5.3.1 başlığı "Çalışan / Stajyer tarafı" olduğu hâlde tablo yalnızca
   Employees'e bakıyordu; stajyer izin talebi giremezdi.

   Çözüm: EmployeeId ve InternId sütunlarının ikisi de nullable olur, CHECK
   kısıtı "tam olarak biri dolu" kuralını garanti eder. Bu kural uygulamada
   da yazılacak, ama asıl güvence veritabanındadır — uygulama dışından ya da
   hatalı bir kod yolundan iki alanı birden (veya hiçbirini) dolduran kayıt
   giremez.

   Alternatif "tek tablo + OwnerType/OwnerId" yaklaşımı reddedildi: o modelde
   foreign key kurulamaz, yani veritabanı Id'nin gerçekten var olduğunu
   doğrulayamaz.
   ============================================================================= */

/* EmployeeId üzerindeki index, ALTER COLUMN'u engeller — önce düşür. */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_EmployeeId')
    DROP INDEX IX_LeaveRequests_EmployeeId ON dbo.LeaveRequests;
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.LeaveRequests')
             AND name = 'EmployeeId' AND is_nullable = 0)
    ALTER TABLE dbo.LeaveRequests ALTER COLUMN EmployeeId int NULL;
GO

IF COL_LENGTH('dbo.LeaveRequests', 'InternId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD InternId int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_Interns')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT FK_LeaveRequests_Interns
        FOREIGN KEY (InternId) REFERENCES dbo.Interns (Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LeaveRequests_Requester')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT CK_LeaveRequests_Requester CHECK
    (
        (EmployeeId IS NOT NULL AND InternId IS NULL)
        OR
        (EmployeeId IS NULL AND InternId IS NOT NULL)
    );
GO

/* ---------------------------------------------------------------------------
   İKİ AŞAMALI ONAY

   Akış:  Beklemede(1) ─yönetici─▶ İK bekliyor(2) ─İK─▶ Onaylandı(3)
                    └──────── reddedilirse ────────▶ Reddedildi(4)

   Her aşamanın izi AYRI sütun çiftinde tutulur. Tek bir "ReviewedBy" alanı
   yeterli olmazdı: iki farklı kişinin iki farklı zamandaki kararı kaydedilmeli
   ki "aynı kişi iki aşamayı da onaylamasın" kuralı denetlenebilsin.

   Kim onayladı bilgisi Users'a bakar, Employees'e değil: onaylayan İK
   personelinin Employee kaydı olmayabilir ama hesabı mutlaka vardır.

   Ayrı bir "onay adımları" tablosu tercih edilmedi: akışın tam olarak iki
   sabit aşaması var, tablo N aşamalı bir esnekliği modellerdi — kullanmayacağımız.
   --------------------------------------------------------------------------- */

/* Önceki tasarımda tek aşamalı iz planlanmıştı; kurulduysa kaldır. */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_ReviewedBy')
    ALTER TABLE dbo.LeaveRequests DROP CONSTRAINT FK_LeaveRequests_ReviewedBy;
GO
IF COL_LENGTH('dbo.LeaveRequests', 'ReviewedByUserId') IS NOT NULL
    ALTER TABLE dbo.LeaveRequests DROP COLUMN ReviewedByUserId, ReviewedAt;
GO

IF COL_LENGTH('dbo.LeaveRequests', 'ManagerApprovedByUserId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD
        ManagerApprovedByUserId int          NULL,   -- 1. aşama: yönetici / mentor
        ManagerApprovedAt       datetime2(0) NULL,
        HrApprovedByUserId      int          NULL,   -- 2. aşama: İK
        HrApprovedAt            datetime2(0) NULL,
        RejectedByUserId        int          NULL,   -- hangi aşamada olursa olsun
        RejectedAt              datetime2(0) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_ManagerApprovedBy')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT FK_LeaveRequests_ManagerApprovedBy
        FOREIGN KEY (ManagerApprovedByUserId) REFERENCES dbo.Users (Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_HrApprovedBy')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT FK_LeaveRequests_HrApprovedBy
        FOREIGN KEY (HrApprovedByUserId) REFERENCES dbo.Users (Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_RejectedBy')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT FK_LeaveRequests_RejectedBy
        FOREIGN KEY (RejectedByUserId) REFERENCES dbo.Users (Id);
GO

/* ---------------------------------------------------------------------------
   VERİ TAŞIMA — Status numaraları değişti

   Eski:  1=Pending  2=Approved  3=Rejected
   Yeni:  1=Pending  2=PendingHr  3=Approved  4=Rejected

   SIRA KRİTİK. Önce 3→4, sonra 2→3 yapılmalıdır. Ters sırada yapılsaydı
   Approved(2) önce 3'e taşınır, ardından ikinci UPDATE onu 4'e götürür ve
   onaylanmış tüm izinler "reddedildi" hâline gelirdi.

   Tek seferlik çalışır: DF_LeaveRequests_StatusMigrated işaretiyle korunur.
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
               WHERE name = 'StatusMigratedToTwoStage'
                 AND major_id = OBJECT_ID('dbo.LeaveRequests'))
BEGIN
    UPDATE dbo.LeaveRequests SET Status = 4 WHERE Status = 3;   -- Rejected  3 → 4
    UPDATE dbo.LeaveRequests SET Status = 3 WHERE Status = 2;   -- Approved  2 → 3

    EXEC sp_addextendedproperty
         @name = N'StatusMigratedToTwoStage', @value = N'2026-07-22',
         @level0type = N'SCHEMA', @level0name = N'dbo',
         @level1type = N'TABLE',  @level1name = N'LeaveRequests';

    PRINT 'OK: LeaveRequests.Status iki asamali akisa tasindi';
END
ELSE
    PRINT 'ATLANDI: Status tasimasi zaten yapilmis';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_EmployeeId')
    CREATE INDEX IX_LeaveRequests_EmployeeId ON dbo.LeaveRequests (EmployeeId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_InternId')
    CREATE INDEX IX_LeaveRequests_InternId ON dbo.LeaveRequests (InternId);
GO

/* "Bekleyen talepler" ekranı (§5.3.2) her açılışta Status'e göre filtreler. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_Status')
    CREATE INDEX IX_LeaveRequests_Status ON dbo.LeaveRequests (Status);
GO


/* =============================================================================
   4) EmployeeNotes — §5.2 "çalışana ait notlar"

   Notu YAZAN kişi Users'a bağlanır, Employees'e değil: not girecek olan
   HR uzmanının sistemde bir Employee kaydı olmayabilir ama hesabı vardır.
   "Bu işlemi kim yaptı" sorusunun cevabı her zaman hesaptır.
   ============================================================================= */
IF OBJECT_ID('dbo.EmployeeNotes', 'U') IS NULL
CREATE TABLE dbo.EmployeeNotes
(
    Id           int            IDENTITY(1,1) NOT NULL,
    EmployeeId   int            NOT NULL,
    AuthorUserId int            NOT NULL,
    Content      nvarchar(1000) NOT NULL,
    CreatedAt    datetime2(0)   NOT NULL CONSTRAINT DF_EmployeeNotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)   NULL,

    CONSTRAINT PK_EmployeeNotes            PRIMARY KEY (Id),
    CONSTRAINT FK_EmployeeNotes_Employees  FOREIGN KEY (EmployeeId)   REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_EmployeeNotes_Users      FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeNotes_EmployeeId')
    CREATE INDEX IX_EmployeeNotes_EmployeeId ON dbo.EmployeeNotes (EmployeeId);
GO


/* =============================================================================
   5) InternTasks — §5.4 "staj süresince verilen görevlerin kısa listesi"

   Doküman "elle girilen basit text alanı yeterli" diyor. Yine de ayrı tablo
   tercih edildi: tek bir metin alanında görevler ne sıralanabilir, ne
   tamamlandı işaretlenebilir, ne de sayılabilir. Maliyeti bir tablo.

   Status → Domain/Enums/InternTaskStatus.cs : 1=Pending 2=InProgress 3=Done
   ============================================================================= */
IF OBJECT_ID('dbo.InternTasks', 'U') IS NULL
CREATE TABLE dbo.InternTasks
(
    Id              int            IDENTITY(1,1) NOT NULL,
    InternId        int            NOT NULL,
    Title           nvarchar(200)  NOT NULL,
    Description     nvarchar(1000) NULL,
    Status          int            NOT NULL CONSTRAINT DF_InternTasks_Status DEFAULT (1),
    DueDate         date           NULL,
    CreatedByUserId int            NOT NULL,
    CreatedAt       datetime2(0)   NOT NULL CONSTRAINT DF_InternTasks_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       datetime2(0)   NULL,

    CONSTRAINT PK_InternTasks          PRIMARY KEY (Id),
    CONSTRAINT FK_InternTasks_Interns  FOREIGN KEY (InternId)        REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_InternTasks_Users    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InternTasks_InternId')
    CREATE INDEX IX_InternTasks_InternId ON dbo.InternTasks (InternId);
GO


/* =============================================================================
   6) InternNotes — §5.4 "mentor notları (haftalık kısa geri bildirimler)"

   EmployeeNotes ile yapı olarak aynıdır ama AYRI tablodur. Tek tabloda
   birleştirmek foreign key'i imkânsız kılardı (bkz. bölüm 3 açıklaması).
   ============================================================================= */
IF OBJECT_ID('dbo.InternNotes', 'U') IS NULL
CREATE TABLE dbo.InternNotes
(
    Id           int            IDENTITY(1,1) NOT NULL,
    InternId     int            NOT NULL,
    AuthorUserId int            NOT NULL,
    Content      nvarchar(1000) NOT NULL,
    CreatedAt    datetime2(0)   NOT NULL CONSTRAINT DF_InternNotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)   NULL,

    CONSTRAINT PK_InternNotes          PRIMARY KEY (Id),
    CONSTRAINT FK_InternNotes_Interns  FOREIGN KEY (InternId)     REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_InternNotes_Users    FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InternNotes_InternId')
    CREATE INDEX IX_InternNotes_InternId ON dbo.InternNotes (InternId);
GO


/* =============================================================================
   7) İZİN HAKKI — §5.5 "kalan yıllık izin gün sayısı"

   AYRI TABLO YOK. Hak, saklanan değil HESAPLANAN bir büyüklüktür:
   kıdem zaten HireDate'ten bilinir, kanun tablosu sabittir.

     Kazanılmış hak (İş Kanunu md. 53, kıdeme göre)
        < 1 yıl  →  0 gün        1–5 yıl  → 14 gün
        5–15 yıl → 20 gün        15+ yıl  → 26 gün

     Kullanılan = geçerli hak döneminde onaylanmış + BEKLEYEN Annual günler
     Kalan      = hak − kullanılan            (avans izinde negatif olabilir)

   Hak dönemi takvim yılı değil, İŞE GİRİŞ YILDÖNÜMÜDÜR:
   1 Temmuz'da işe giren, 1 Ocak'ta değil ertesi 1 Temmuz'da hak kazanır.

   Aşağıdaki sütun yalnızca ELLE GEÇERSİZ KILMA içindir:
     NULL  → kanundan hesapla (normal durum)
     dolu  → şirket bu kişiye özel gün tanımlamış, hesaplamayı ez

   Stajyerlere sütun eklenmez: staj 1 yıldan kısadır, kazanılmış yıllık izin
   hakları her hâlükârda 0'dır. İzin ihtiyaçları Unpaid/Sick türleriyle karşılanır.
   ============================================================================= */
IF COL_LENGTH('dbo.Employees', 'AnnualLeaveDays') IS NULL
    ALTER TABLE dbo.Employees ADD AnnualLeaveDays int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_AnnualLeaveDays')
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_AnnualLeaveDays
        CHECK (AnnualLeaveDays IS NULL OR AnnualLeaveDays >= 0);
GO

/* Eski tasarımda ayrı tablo planlanmıştı; kurulduysa kaldır (içi boştu). */
IF OBJECT_ID('dbo.LeaveBalances', 'U') IS NOT NULL
    DROP TABLE dbo.LeaveBalances;
GO

PRINT 'IK modulu semasi hazir.';
GO
