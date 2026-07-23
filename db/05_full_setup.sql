/* =============================================================================
   HRManagement — TAM KURULUM   (2026-07-22)

   TEK DOSYA. 01–04'ün tamamını içerir; onları ayrıca çalıştırmana gerek yok.

   İki durumda da doğru sonucu verir:
     • Sıfırdan kurulum  → tabloları nihai şemasıyla oluşturur
     • Mevcut veritabanı → eksik sütun/kısıt/index'i ekler, veriyi taşır

   Tekrar çalıştırılabilir (idempotent): her adım "zaten var mı" diye bakar.
   Engelleyici veri varsa sessizce geçmez, durup bildirir.

   BÖLÜMLER
     0  Veritabanı
     1  Tablolar (nihai şema)
     2  Mevcut tablolara eksik sütunlar
     3  Kısıtlar (NOT NULL / UNIQUE / CHECK / FK)
     4  Index'ler
     5  Veri taşıma — LeaveRequests.Status
     6  Geliştirme verisi (seed)
     7  Doğrulama raporu
   ============================================================================= */

SET NOCOUNT ON;
GO

/* =============================================================================
   0) VERİTABANI
   ============================================================================= */
IF DB_ID('HRManagementDb') IS NULL
BEGIN
    CREATE DATABASE HRManagementDb;
    PRINT 'OK: HRManagementDb olusturuldu';
END
ELSE
    PRINT 'ATLANDI: HRManagementDb zaten var';
GO

USE HRManagementDb;
GO


/* =============================================================================
   1) TABLOLAR — nihai şema

   Sıfırdan kurulumda buradaki tanımlar geçerlidir ve 2. bölümdeki ALTER'lar
   boşa düşer. Mevcut veritabanında ise bu bölüm atlanır, işi 2. bölüm yapar.

   Enum karşılıkları (Domain/Enums):
     Users.Role            1=Admin 2=HR 3=Manager 4=Employee 5=Intern
     LeaveRequests.Type    1=Annual 2=Unpaid 3=Sick
     LeaveRequests.Status  1=Pending 2=PendingHr 3=Approved 4=Rejected
     InternTasks.Status    1=Pending 2=InProgress 3=Done

   Tarihler UTC tutulur (SYSUTCDATETIME): sunucunun saat dilimi değişirse
   karşılaştırmalar bozulmasın diye. Kullanıcıya gösterilirken çevrilir.
   UpdatedAt null bırakılır — "hiç güncellenmedi" bilgisi korunsun diye.
   ============================================================================= */

IF OBJECT_ID('dbo.Departments', 'U') IS NULL
CREATE TABLE dbo.Departments
(
    Id          int           IDENTITY(1,1) NOT NULL,
    Name        nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,
    CreatedAt   datetime2(0)  NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   datetime2(0)  NULL,

    CONSTRAINT PK_Departments PRIMARY KEY (Id)
);
GO

/* PasswordHash nvarchar(255): BCrypt hash'i 60 karakterdir. Sütun dar olursa
   hash sessizce kırpılır ve giriş HİÇ çalışmaz — bulması çok zor bir hatadır. */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users
(
    Id           int           IDENTITY(1,1) NOT NULL,
    Username     nvarchar(50)  NOT NULL,
    Email        nvarchar(100) NOT NULL,
    PasswordHash nvarchar(255) NOT NULL,
    Role         int           NOT NULL,
    IsActive     bit           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt    datetime2(0)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)  NULL,

    CONSTRAINT PK_Users          PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email)
);
GO

/* DateOfBirth NOT NULL olmak zorunda: Employee.DateOfBirth C# tarafında
   nullable değil. Tek bir NULL satır Dapper map'ini bozar ve yalnızca o
   satırı değil TÜM çalışan listesini 500 yapar.

   ManagerId kendine referans verir (yöneticinin kendisi de bir çalışandır).
   AnnualLeaveDays yalnızca elle geçersiz kılma içindir; normalde NULL kalır
   ve izin hakkı HireDate'ten kıdem hesaplanarak bulunur. */
IF OBJECT_ID('dbo.Employees', 'U') IS NULL
CREATE TABLE dbo.Employees
(
    Id              int           IDENTITY(1,1) NOT NULL,
    FirstName       nvarchar(50)  NOT NULL,
    LastName        nvarchar(50)  NOT NULL,
    NationalId      nvarchar(11)  NULL,
    DateOfBirth     date          NOT NULL,
    DepartmentId    int           NOT NULL,
    HireDate        date          NOT NULL,
    Email           nvarchar(100) NOT NULL,
    Phone           nvarchar(20)  NULL,
    IsActive        bit           NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    UserId          int           NULL,
    ManagerId       int           NULL,
    AnnualLeaveDays int           NULL,
    Seniority       int           NULL,   -- 1=GM 2=GMY 3=Müdür 4=MüdürYrd 5=Kıd.Uzman 6=Uzman
    CreatedAt       datetime2(0)  NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       datetime2(0)  NULL,

    CONSTRAINT PK_Employees                  PRIMARY KEY (Id),
    CONSTRAINT UQ_Employees_Email            UNIQUE (Email),
    CONSTRAINT CK_Employees_AnnualLeaveDays  CHECK (AnnualLeaveDays IS NULL OR AnnualLeaveDays >= 0),
    CONSTRAINT CK_Employees_Seniority        CHECK (Seniority IS NULL OR Seniority BETWEEN 1 AND 6),
    CONSTRAINT FK_Employees_Departments      FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_Employees_Users            FOREIGN KEY (UserId)       REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Employees_Manager          FOREIGN KEY (ManagerId)    REFERENCES dbo.Employees (Id)
);
GO

IF OBJECT_ID('dbo.Interns', 'U') IS NULL
CREATE TABLE dbo.Interns
(
    Id           int           IDENTITY(1,1) NOT NULL,
    FirstName    nvarchar(50)  NOT NULL,
    LastName     nvarchar(50)  NOT NULL,
    Email        nvarchar(100) NOT NULL,
    University   nvarchar(150) NOT NULL,
    Major        nvarchar(100) NOT NULL,
    Grade        int           NOT NULL,
    StartDate    date          NOT NULL,
    EndDate      date          NOT NULL,
    MentorId     int           NULL,
    DepartmentId int           NOT NULL,
    UserId       int           NULL,
    CreatedAt    datetime2(0)  NOT NULL CONSTRAINT DF_Interns_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)  NULL,

    CONSTRAINT PK_Interns             PRIMARY KEY (Id),
    CONSTRAINT FK_Interns_Employees   FOREIGN KEY (MentorId)     REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_Interns_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_Interns_Users       FOREIGN KEY (UserId)       REFERENCES dbo.Users (Id)
);
GO

/* Talep sahibi ya çalışan ya stajyerdir (§5.3.1 "Çalışan / Stajyer tarafı").
   CHECK kısıtı "tam olarak biri dolu" kuralını GARANTİ eder — uygulama
   dışından ya da hatalı bir kod yolundan bozuk kayıt giremez.

   "OwnerType + OwnerId" tek sütun çifti yaklaşımı reddedildi: o modelde
   foreign key kurulamaz, yani veritabanı Id'nin varlığını doğrulayamaz.

   Onayın iki aşaması AYRI sütun çiftlerinde izlenir; tek bir "ReviewedBy"
   alanı "aynı kişi iki aşamayı da onaylamasın" kuralını denetlemeye yetmezdi. */
IF OBJECT_ID('dbo.LeaveRequests', 'U') IS NULL
CREATE TABLE dbo.LeaveRequests
(
    Id                      int           IDENTITY(1,1) NOT NULL,
    EmployeeId              int           NULL,
    InternId                int           NULL,
    Type                    int           NOT NULL,
    StartDate               date          NOT NULL,
    EndDate                 date          NOT NULL,
    Description             nvarchar(500) NULL,
    Status                  int           NOT NULL CONSTRAINT DF_LeaveRequests_Status DEFAULT (1),
    RejectionReason         nvarchar(500) NULL,
    ManagerApprovedByUserId int           NULL,
    ManagerApprovedAt       datetime2(0)  NULL,
    HrApprovedByUserId      int           NULL,
    HrApprovedAt            datetime2(0)  NULL,
    RejectedByUserId        int           NULL,
    RejectedAt              datetime2(0)  NULL,
    CreatedAt               datetime2(0)  NOT NULL CONSTRAINT DF_LeaveRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt               datetime2(0)  NULL,

    CONSTRAINT PK_LeaveRequests                 PRIMARY KEY (Id),
    CONSTRAINT CK_LeaveRequests_Requester CHECK
    (
        (EmployeeId IS NOT NULL AND InternId IS NULL)
        OR
        (EmployeeId IS NULL AND InternId IS NOT NULL)
    ),
    CONSTRAINT FK_LeaveRequests_Employees          FOREIGN KEY (EmployeeId)              REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_LeaveRequests_Interns            FOREIGN KEY (InternId)                REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_LeaveRequests_ManagerApprovedBy  FOREIGN KEY (ManagerApprovedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_LeaveRequests_HrApprovedBy       FOREIGN KEY (HrApprovedByUserId)      REFERENCES dbo.Users (Id),
    CONSTRAINT FK_LeaveRequests_RejectedBy         FOREIGN KEY (RejectedByUserId)        REFERENCES dbo.Users (Id)
);
GO

/* §5.2 — çalışan detay ekranındaki notlar.
   Notu YAZAN Users'a bağlanır, Employees'e değil: not girecek İK uzmanının
   çalışan kaydı olmayabilir ama hesabı mutlaka vardır. "Bu işlemi kim yaptı"
   sorusunun cevabı her zaman hesaptır. */
IF OBJECT_ID('dbo.EmployeeNotes', 'U') IS NULL
CREATE TABLE dbo.EmployeeNotes
(
    Id           int            IDENTITY(1,1) NOT NULL,
    EmployeeId   int            NOT NULL,
    AuthorUserId int            NOT NULL,
    Content      nvarchar(1000) NOT NULL,
    CreatedAt    datetime2(0)   NOT NULL CONSTRAINT DF_EmployeeNotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)   NULL,

    CONSTRAINT PK_EmployeeNotes           PRIMARY KEY (Id),
    CONSTRAINT FK_EmployeeNotes_Employees FOREIGN KEY (EmployeeId)   REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_EmployeeNotes_Users     FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users (Id)
);
GO

/* §5.4 — staj görevleri. Doküman "basit text alanı yeterli" dese de ayrı
   tablo tercih edildi: tek metin alanında görevler sıralanamaz, tamamlandı
   işaretlenemez ve sayılamaz. Maliyeti bir tablo. */
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

    CONSTRAINT PK_InternTasks         PRIMARY KEY (Id),
    CONSTRAINT FK_InternTasks_Interns FOREIGN KEY (InternId)        REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_InternTasks_Users   FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
);
GO

/* §5.4 — mentor notları. EmployeeNotes ile yapı olarak aynıdır ama AYRI
   tablodur: tek tabloda birleştirmek foreign key kurmayı imkânsız kılardı. */
IF OBJECT_ID('dbo.InternNotes', 'U') IS NULL
CREATE TABLE dbo.InternNotes
(
    Id           int            IDENTITY(1,1) NOT NULL,
    InternId     int            NOT NULL,
    AuthorUserId int            NOT NULL,
    Content      nvarchar(1000) NOT NULL,
    CreatedAt    datetime2(0)   NOT NULL CONSTRAINT DF_InternNotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)   NULL,

    CONSTRAINT PK_InternNotes         PRIMARY KEY (Id),
    CONSTRAINT FK_InternNotes_Interns FOREIGN KEY (InternId)     REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_InternNotes_Users   FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users (Id)
);
GO

/* Hesap açma talepleri: HR talep eder, Admin işler. Şifre burada TUTULMAZ.
   Status: 1=Pending 2=Approved 3=Rejected. EmployeeId/InternId tam biri dolu. */
IF OBJECT_ID('dbo.AccountRequests', 'U') IS NULL
CREATE TABLE dbo.AccountRequests
(
    Id                int           IDENTITY(1,1) NOT NULL,
    EmployeeId        int           NULL,
    InternId          int           NULL,
    RequestedByUserId int           NOT NULL,
    SuggestedRole     int           NOT NULL,
    Note              nvarchar(500) NULL,
    Status            int           NOT NULL CONSTRAINT DF_AccountRequests_Status DEFAULT (1),
    RejectionReason   nvarchar(500) NULL,
    ReviewedByUserId  int           NULL,
    ReviewedAt        datetime2(0)  NULL,
    CreatedAt         datetime2(0)  NOT NULL CONSTRAINT DF_AccountRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt         datetime2(0)  NULL,

    CONSTRAINT PK_AccountRequests PRIMARY KEY (Id),
    CONSTRAINT CK_AccountRequests_Subject CHECK
    (
        (EmployeeId IS NOT NULL AND InternId IS NULL)
        OR
        (EmployeeId IS NULL AND InternId IS NOT NULL)
    ),
    CONSTRAINT FK_AccountRequests_Employees   FOREIGN KEY (EmployeeId)        REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_AccountRequests_Interns     FOREIGN KEY (InternId)          REFERENCES dbo.Interns (Id),
    CONSTRAINT FK_AccountRequests_RequestedBy FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_AccountRequests_ReviewedBy  FOREIGN KEY (ReviewedByUserId)  REFERENCES dbo.Users (Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AccountRequests_PendingEmployee')
    CREATE UNIQUE INDEX UX_AccountRequests_PendingEmployee
        ON dbo.AccountRequests (EmployeeId) WHERE Status = 1 AND EmployeeId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AccountRequests_PendingIntern')
    CREATE UNIQUE INDEX UX_AccountRequests_PendingIntern
        ON dbo.AccountRequests (InternId) WHERE Status = 1 AND InternId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AccountRequests_Status')
    CREATE INDEX IX_AccountRequests_Status ON dbo.AccountRequests (Status);
GO


/* =============================================================================
   2) MEVCUT TABLOLARA EKSİK SÜTUNLAR

   Sıfırdan kurulumda bu bölümün tamamı atlanır (sütunlar zaten oluştu).
   ============================================================================= */

/* --- Audit sütunları --- */
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

/* --- Employees: ekip, izin hakkı ve kıdem --- */
IF COL_LENGTH('dbo.Employees', 'ManagerId') IS NULL
    ALTER TABLE dbo.Employees ADD ManagerId int NULL;
GO
IF COL_LENGTH('dbo.Employees', 'AnnualLeaveDays') IS NULL
    ALTER TABLE dbo.Employees ADD AnnualLeaveDays int NULL;
GO
IF COL_LENGTH('dbo.Employees', 'Seniority') IS NULL
    ALTER TABLE dbo.Employees ADD Seniority int NULL;
GO

/* --- Position artık türetiliyor (Departman + Kıdem); sütun kaldırılır --- */
IF COL_LENGTH('dbo.Employees', 'Position') IS NOT NULL
    ALTER TABLE dbo.Employees DROP COLUMN Position;
GO

/* --- LeaveRequests: stajyer talepleri --- */
IF COL_LENGTH('dbo.LeaveRequests', 'InternId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD InternId int NULL;
GO

/* --- LeaveRequests: eski tek aşamalı iz varsa kaldır --- */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_ReviewedBy')
    ALTER TABLE dbo.LeaveRequests DROP CONSTRAINT FK_LeaveRequests_ReviewedBy;
GO
IF COL_LENGTH('dbo.LeaveRequests', 'ReviewedByUserId') IS NOT NULL
    ALTER TABLE dbo.LeaveRequests DROP COLUMN ReviewedByUserId, ReviewedAt;
GO

/* --- LeaveRequests: iki aşamalı onay izi --- */
IF COL_LENGTH('dbo.LeaveRequests', 'ManagerApprovedByUserId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD
        ManagerApprovedByUserId int          NULL,
        ManagerApprovedAt       datetime2(0) NULL,
        HrApprovedByUserId      int          NULL,
        HrApprovedAt            datetime2(0) NULL,
        RejectedByUserId        int          NULL,
        RejectedAt              datetime2(0) NULL;
GO

/* --- Önceki tasarımda planlanan ayrı bakiye tablosu varsa kaldır --- */
IF OBJECT_ID('dbo.LeaveBalances', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.LeaveBalances;
    PRINT 'OK: LeaveBalances kaldirildi (izin hakki artik HireDate''ten hesaplaniyor)';
END
GO


/* =============================================================================
   3) KISITLAR

   Her adım hem "zaten var mı" hem "engelleyici veri var mı" diye bakar.
   Engel varsa DURUR ve bildirir — sessizce veri uydurmak daha kötü olurdu.
   ============================================================================= */

/* --- Employees.DateOfBirth → NOT NULL --- */
IF EXISTS (SELECT 1 FROM dbo.Employees WHERE DateOfBirth IS NULL)
    RAISERROR('DURDURULDU: Employees.DateOfBirth sutununda NULL kayitlar var. Once doldurun: SELECT Id, FirstName, LastName FROM dbo.Employees WHERE DateOfBirth IS NULL;', 16, 1);
ELSE IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Employees') AND name = 'DateOfBirth' AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.Employees ALTER COLUMN DateOfBirth date NOT NULL;
    PRINT 'OK: Employees.DateOfBirth -> NOT NULL';
END
GO

/* --- Employees.Email → UNIQUE ---
   Benzersizliği uygulama da kontrol ediyor, ama asıl garantiyi kısıt verir:
   uygulama dışı kayıtlar ve eşzamanlı iki istek ancak böyle engellenir. */
IF EXISTS (SELECT Email FROM dbo.Employees GROUP BY Email HAVING COUNT(*) > 1)
    RAISERROR('DURDURULDU: Employees.Email sutununda tekrar eden degerler var. Kontrol: SELECT Email, COUNT(*) FROM dbo.Employees GROUP BY Email HAVING COUNT(*) > 1;', 16, 1);
ELSE IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_Employees_Email')
BEGIN
    ALTER TABLE dbo.Employees ADD CONSTRAINT UQ_Employees_Email UNIQUE (Email);
    PRINT 'OK: UQ_Employees_Email';
END
GO

/* --- Employees.AnnualLeaveDays >= 0 --- */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_AnnualLeaveDays')
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_AnnualLeaveDays
        CHECK (AnnualLeaveDays IS NULL OR AnnualLeaveDays >= 0);
GO

/* --- Employees.Seniority 1..6 (enum aralığı) --- */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_Seniority')
    ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_Seniority
        CHECK (Seniority IS NULL OR Seniority BETWEEN 1 AND 6);
GO

/* --- Employees.ManagerId FK (kendine referans) ---
   SQL Server kendine referans veren FK'da ON DELETE CASCADE'e izin vermez
   (döngü riski). NO ACTION kalır; yöneticisi silinen çalışan kuralı
   Application katmanında ele alınır. */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Manager')
    ALTER TABLE dbo.Employees ADD CONSTRAINT FK_Employees_Manager
        FOREIGN KEY (ManagerId) REFERENCES dbo.Employees (Id);
GO

/* --- LeaveRequests.EmployeeId → NULL yapılabilir ---
   Üzerindeki index ALTER COLUMN'u engeller; önce düşürülür, 4. bölümde
   yeniden kurulur. */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_EmployeeId')
    DROP INDEX IX_LeaveRequests_EmployeeId ON dbo.LeaveRequests;
GO
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.LeaveRequests') AND name = 'EmployeeId' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.LeaveRequests ALTER COLUMN EmployeeId int NULL;
    PRINT 'OK: LeaveRequests.EmployeeId -> NULL yapilabilir';
END
GO

/* --- LeaveRequests: talep sahibi kısıtı --- */
IF EXISTS (SELECT 1 FROM dbo.LeaveRequests WHERE EmployeeId IS NULL AND InternId IS NULL)
    RAISERROR('DURDURULDU: LeaveRequests icinde sahibi olmayan kayitlar var. Kontrol: SELECT * FROM dbo.LeaveRequests WHERE EmployeeId IS NULL AND InternId IS NULL;', 16, 1);
ELSE IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LeaveRequests_Requester')
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT CK_LeaveRequests_Requester CHECK
    (
        (EmployeeId IS NOT NULL AND InternId IS NULL)
        OR
        (EmployeeId IS NULL AND InternId IS NOT NULL)
    );
    PRINT 'OK: CK_LeaveRequests_Requester';
END
GO

/* --- LeaveRequests: yeni FK'lar --- */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_Interns')
    ALTER TABLE dbo.LeaveRequests ADD CONSTRAINT FK_LeaveRequests_Interns
        FOREIGN KEY (InternId) REFERENCES dbo.Interns (Id);
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


/* =============================================================================
   4) INDEX'LER

   SQL Server, PK ve UNIQUE için index'i otomatik kurar ama FOREIGN KEY
   sütunları için KURMAZ. "Bu departmandaki çalışanlar", "bu çalışanın
   izinleri" gibi sorgular bu sütunlar üzerinden filtreler; index olmadan
   her sorgu tam tablo taraması olur.
   ============================================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId')
    CREATE INDEX IX_Employees_DepartmentId ON dbo.Employees (DepartmentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_UserId')
    CREATE INDEX IX_Employees_UserId ON dbo.Employees (UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_ManagerId')
    CREATE INDEX IX_Employees_ManagerId ON dbo.Employees (ManagerId);
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
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_InternId')
    CREATE INDEX IX_LeaveRequests_InternId ON dbo.LeaveRequests (InternId);
GO
/* "Bekleyen talepler" ekranı (§5.3.2) her açılışta Status'e göre filtreler. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequests_Status')
    CREATE INDEX IX_LeaveRequests_Status ON dbo.LeaveRequests (Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeNotes_EmployeeId')
    CREATE INDEX IX_EmployeeNotes_EmployeeId ON dbo.EmployeeNotes (EmployeeId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InternTasks_InternId')
    CREATE INDEX IX_InternTasks_InternId ON dbo.InternTasks (InternId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InternNotes_InternId')
    CREATE INDEX IX_InternNotes_InternId ON dbo.InternNotes (InternId);
GO


/* =============================================================================
   5) VERİ TAŞIMA — LeaveRequests.Status

   İzin onayı iki aşamalı hâle geldi ve enum numaraları değişti:

       Eski:  1=Pending  2=Approved   3=Rejected
       Yeni:  1=Pending  2=PendingHr  3=Approved  4=Rejected

   SIRA KRİTİK: önce 3→4, SONRA 2→3.
   Ters sırada yapılsaydı Approved(2) önce 3'e taşınır, ardından ikinci
   UPDATE onu 4'e götürürdü — onaylanmış TÜM izinler "reddedildi" olurdu.

   Tek seferlik çalışır; işaret extended property olarak tabloya yazılır.
   ============================================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
               WHERE name = 'StatusMigratedToTwoStage'
                 AND major_id = OBJECT_ID('dbo.LeaveRequests'))
BEGIN
    UPDATE dbo.LeaveRequests SET Status = 4 WHERE Status = 3;   -- Rejected 3 → 4
    UPDATE dbo.LeaveRequests SET Status = 3 WHERE Status = 2;   -- Approved 2 → 3

    EXEC sp_addextendedproperty
         @name = N'StatusMigratedToTwoStage', @value = N'2026-07-22',
         @level0type = N'SCHEMA', @level0name = N'dbo',
         @level1type = N'TABLE',  @level1name = N'LeaveRequests';

    PRINT 'OK: LeaveRequests.Status iki asamali akisa tasindi';
END
ELSE
    PRINT 'ATLANDI: Status tasimasi zaten yapilmis';
GO


/* =============================================================================
   6) GELİŞTİRME VERİSİ

   UYARI: Bu bölüm CANLI ortamda çalıştırılmamalıdır. Şifre bilinçli olarak
   zayıftır ve hash'i herkese açık bir repoda durur.

   Neden gerekli (tavuk-yumurta): hesapları yalnızca giriş yapmış yetkililer
   açabiliyor, ama Users tablosu boşken kimse giriş yapamaz.

   Hash, projedeki PasswordHasher ile aynı çağrıyla üretildi:
       BCrypt.Net.BCrypt.HashPassword("12341234")   → work factor 11
   BCrypt her çağrıda rastgele salt ürettiği için bu değer yeniden
   üretilemez; salt hash'in içinde taşınır, ayrı sütuna gerek yoktur.
   ============================================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'test@test.com' OR Username = 'test')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, Role, IsActive)
    VALUES ('test', 'test@test.com',
            '$2a$11$pk4mPVyeCXrW/gghk.JkzO8nJ0Q49Rx1susTqLwneOMBTwesrrzpW',
            1, 1);   -- Role.Admin, aktif
    PRINT 'OK: test admin olusturuldu (test@test.com / 12341234)';
END
ELSE
    PRINT 'ATLANDI: test admin zaten var';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Yazılım')
    INSERT INTO dbo.Departments (Name, Description)
    VALUES (N'Yazılım', N'Yazılım geliştirme departmanı');
GO


/* =============================================================================
   7) DOĞRULAMA RAPORU
   ============================================================================= */
PRINT '';
PRINT '================ KURULUM TAMAMLANDI ================';
GO

SELECT t.name AS Tablo, SUM(p.rows) AS SatirSayisi
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name;

SELECT 'Kullanicilar' AS Bolum, Id, Username, Email, Role, IsActive,
       LEN(PasswordHash) AS HashUzunlugu   -- 60 bekleniyor
FROM dbo.Users;

SELECT 'Kisitlar' AS Bolum, name AS Ad, type_desc AS Tur
FROM sys.objects
WHERE type IN ('C','F','UQ') AND is_ms_shipped = 0
ORDER BY type_desc, name;
GO
