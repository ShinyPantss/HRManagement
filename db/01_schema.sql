/* =============================================================================
   HRManagement — veritabanı şeması

   Proje Dapper kullanıyor, EF Core YOK. Dolayısıyla migration da yok:
   şemanın tek doğruluk kaynağı BU DOSYADIR. Her şema değişikliği buraya
   işlenir ve git'te takip edilir.

   Kurulum:  01_schema.sql  →  02_seed_dev.sql

   Script yeniden çalıştırılabilir (idempotent): var olan nesnelere dokunmaz.
   ============================================================================= */

IF DB_ID('HRManagementDb') IS NULL
    CREATE DATABASE HRManagementDb;
GO

USE HRManagementDb;
GO


/* --- Departments -----------------------------------------------------------
   Bağımlılığı olmayan ilk tablo; Employees ve Interns buraya bakar.          */
IF OBJECT_ID('dbo.Departments', 'U') IS NULL
CREATE TABLE dbo.Departments
(
    Id          int           IDENTITY(1,1) NOT NULL,
    Name        nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,

    CONSTRAINT PK_Departments PRIMARY KEY (Id)
);
GO


/* --- Users -----------------------------------------------------------------
   Role sütunu Domain/Enums/Role.cs karşılığıdır:
     1=Admin  2=HR  3=Manager  4=Employee  5=Intern

   PasswordHash nvarchar(255): BCrypt hash'i 60 karakterdir, bolca yer var.
   Bu sütun dar olursa hash sessizce kırpılır ve giriş hiç çalışmaz.          */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users
(
    Id           int           IDENTITY(1,1) NOT NULL,
    Username     nvarchar(50)  NOT NULL,
    Email        nvarchar(100) NOT NULL,
    PasswordHash nvarchar(255) NOT NULL,
    Role         int           NOT NULL,
    IsActive     bit           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),

    CONSTRAINT PK_Users        PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email)
);
GO


/* --- Employees -------------------------------------------------------------
   UserId NULL olabilir: her çalışanın sisteme giriş hesabı olmak zorunda
   değildir (Gereksinim 5.2). Hesap sonradan bağlanabilir.                    */
IF OBJECT_ID('dbo.Employees', 'U') IS NULL
CREATE TABLE dbo.Employees
(
    Id           int           IDENTITY(1,1) NOT NULL,
    FirstName    nvarchar(50)  NOT NULL,
    LastName     nvarchar(50)  NOT NULL,
    NationalId   nvarchar(11)  NULL,
    -- NOT NULL olmak ZORUNDA: Employee.DateOfBirth C# tarafında nullable değil.
    -- Tek bir NULL satır Dapper map'ini bozar ve tüm çalışan listesini 500 yapar.
    DateOfBirth  date          NOT NULL,
    DepartmentId int           NOT NULL,
    Position     nvarchar(100) NOT NULL,
    HireDate     date          NOT NULL,
    Email        nvarchar(100) NOT NULL,
    Phone        nvarchar(20)  NULL,
    IsActive     bit           NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    UserId       int           NULL,

    CONSTRAINT PK_Employees              PRIMARY KEY (Id),
    -- Benzersizliği uygulama da kontrol ediyor, ama asıl garantiyi kısıt verir:
    -- uygulama dışı kayıtlar ve eşzamanlı istekler ancak böyle engellenir.
    CONSTRAINT UQ_Employees_Email        UNIQUE (Email),
    CONSTRAINT FK_Employees_Departments  FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_Employees_Users        FOREIGN KEY (UserId)       REFERENCES dbo.Users (Id)
);
GO


/* --- Interns ---------------------------------------------------------------
   MentorId bir Employee'ye işaret eder ve NULL olabilir (mentor sonradan atanır). */
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

    CONSTRAINT PK_Interns             PRIMARY KEY (Id),
    CONSTRAINT FK_Interns_Employees   FOREIGN KEY (MentorId)     REFERENCES dbo.Employees (Id),
    CONSTRAINT FK_Interns_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_Interns_Users       FOREIGN KEY (UserId)       REFERENCES dbo.Users (Id)
);
GO


/* --- LeaveRequests ---------------------------------------------------------
   Type   → Domain/Enums/LeaveType.cs    : 1=Annual  2=Unpaid  3=Sick
   Status → Domain/Enums/LeaveStatus.cs  : 1=Pending 2=Approved 3=Rejected
   RejectionReason yalnızca Status=3 iken doldurulur (kural handler'da).      */
IF OBJECT_ID('dbo.LeaveRequests', 'U') IS NULL
CREATE TABLE dbo.LeaveRequests
(
    Id              int           IDENTITY(1,1) NOT NULL,
    EmployeeId      int           NOT NULL,
    Type            int           NOT NULL,
    StartDate       date          NOT NULL,
    EndDate         date          NOT NULL,
    Description     nvarchar(500) NULL,
    Status          int           NOT NULL CONSTRAINT DF_LeaveRequests_Status DEFAULT (1),
    RejectionReason nvarchar(500) NULL,

    CONSTRAINT PK_LeaveRequests            PRIMARY KEY (Id),
    CONSTRAINT FK_LeaveRequests_Employees  FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees (Id)
);
GO


/* --- Index'ler -------------------------------------------------------------
   SQL Server, PK ve UNIQUE için index'i otomatik kurar ama FOREIGN KEY
   sütunları için KURMAZ. "Bu departmandaki çalışanlar" gibi sorgular bu
   sütunlar üzerinden filtreler; index'siz her sorgu tam tablo taraması olur. */

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
