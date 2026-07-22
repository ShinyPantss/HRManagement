/* =============================================================================
   HRManagement — Hesap açma talepleri   (2026-07-22)

   Mevcut veritabanına AccountRequests tablosunu ekler. Sıfırdan kurulumda
   05_full_setup.sql zaten bu tabloyu içerir; bu dosya yalnızca DELTA'dır.
   Tekrar çalıştırılabilir.

   Akış: HR talep eder → Admin onaylar (hesap açılır) / reddeder.
   Status: 1=Pending 2=Approved 3=Rejected   (Domain/Enums/AccountRequestStatus.cs)
   ============================================================================= */

USE HRManagementDb;
GO

/* EmployeeId/InternId: TAM OLARAK biri dolu (CHECK). Şifre burada TUTULMAZ —
   Admin onaylarken belirler. RequestedByUserId denetim izidir. */
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

/* FILTERED UNIQUE INDEX: bir kişiye aynı anda TEK "bekleyen" talep.
   WHERE Status=1 sayesinde onaylanmış/reddedilmiş eski talepler engel olmaz;
   kişiye yeni talep ancak öncekiler kapandıysa açılabilir. Bu garanti DB'de
   durur — uygulama kontrolü atlansa bile mükerrer bekleyen talep giremez. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AccountRequests_PendingEmployee')
    CREATE UNIQUE INDEX UX_AccountRequests_PendingEmployee
        ON dbo.AccountRequests (EmployeeId)
        WHERE Status = 1 AND EmployeeId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AccountRequests_PendingIntern')
    CREATE UNIQUE INDEX UX_AccountRequests_PendingIntern
        ON dbo.AccountRequests (InternId)
        WHERE Status = 1 AND InternId IS NOT NULL;
GO

/* "Bekleyen talepler" ekranı Status'e göre filtreler. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AccountRequests_Status')
    CREATE INDEX IX_AccountRequests_Status ON dbo.AccountRequests (Status);
GO

PRINT 'OK: AccountRequests hazir.';
GO
