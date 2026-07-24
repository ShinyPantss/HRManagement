/* =============================================================================
   HRManagement — Birimler (Units): departman alt kırılımı            (2026-07-23)

   Departman → Birim (bire-çok). Her birim bir departmana bağlıdır.
   Kaynak: organizasyon şeması (md). Departmanlar İSİMLE eşleştirilir; böylece
   Id'ler ortama göre değişse bile seed doğru bağlanır.

   Kapsam (şimdilik): birim = yalnızca ad + departman. md'deki *yönetici* ve
   *kişi sayısı* bilgi amaçlı; yönetici sonra Units'e YöneticiId ile, kişi sayısı
   da çalışanları sayarak eklenecek.

   Not / temizlikler:
     - Parantezli ünvanlar (Direktör / Müdür Yrd. / Uzman) YÖNETİCİNİN ünvanıdır,
       birim adı değil → adlardan çıkarıldı. (Direktör, kıdemde Müdür'e eşdeğer sayılır.)
     - Bireysel ünvanlar (birim değil, tek kişilik rol) elimine edildi:
       Uyum Yetkilisi, Hukuk Müşaviri, Yönetim Asistanı, Şoför,
       Kıdemli Hazine Uzmanı, Aktüerya.
     - "Kasko Ürün ve Tarife" md'de iki kez geçiyordu (Uzman + Müdür Yrd.) →
       tek birim olarak alındı.
     - "Müşteri İletişim Merkezi ve Operasyon" md'de kesikti; tam adı farklı olabilir.
     - Yönetim (GM'nin departmanı) için birim yok.

   Tekrar çalıştırılabilir (idempotent). Sıfırdan kurulumda 05_full_setup.sql da içerir.
   ============================================================================= */

USE HRManagementDb;
GO

/* 1) Tablo ------------------------------------------------------------------ */
IF OBJECT_ID('dbo.Units', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Units
    (
        Id           int IDENTITY(1,1) NOT NULL,
        DepartmentId int           NOT NULL,   -- hangi departmanın altında
        Name         nvarchar(200) NOT NULL,   -- "Veri Analitiği ve Mühendisliği" gibi
        CreatedAt    datetime2(0)  NOT NULL CONSTRAINT DF_Units_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt    datetime2(0)  NULL,
        CONSTRAINT PK_Units PRIMARY KEY (Id),
        CONSTRAINT FK_Units_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
        -- Aynı departmanda aynı isimli birim iki kez olamaz.
        CONSTRAINT UQ_Units_Dept_Name UNIQUE (DepartmentId, Name)
    );
    PRINT 'OK: Units tablosu olusturuldu';
END
ELSE
    PRINT 'ATLANDI: Units tablosu zaten var';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Units_DepartmentId')
    CREATE INDEX IX_Units_DepartmentId ON dbo.Units (DepartmentId);
GO

/* 2) Seed ------------------------------------------------------------------ */
-- (DepartmanAdı, BirimAdı) çiftleri; departman isimle bulunur, eksikse o satır
-- sessizce atlanır (departman yoksa hata değil).
;WITH src(DeptName, UnitName) AS
(
    SELECT * FROM (VALUES
        -- Bilgi Teknolojileri (IT)
        (N'Bilgi Teknolojileri', N'Veri Analitiği ve Mühendisliği'),
        (N'Bilgi Teknolojileri', N'Sistem ve Network'),
        (N'Bilgi Teknolojileri', N'Temel Sigortacılık Uygulamaları'),
        (N'Bilgi Teknolojileri', N'Proje ve Süreç Yönetimi'),
        (N'Bilgi Teknolojileri', N'Dijital Platformlar ve Entegrasyon'),
        (N'Bilgi Teknolojileri', N'Müşteri İletişim Merkezi ve Operasyon'),
        (N'Bilgi Teknolojileri', N'Kurumsal Mimariler'),

        -- Hasar
        (N'Hasar', N'Dijital Hasar'),
        (N'Hasar', N'Partner Yönetim'),
        (N'Hasar', N'Oto Ağır Hasar'),
        (N'Hasar', N'Hasar'),
        (N'Hasar', N'Hasar Lojistik'),
        (N'Hasar', N'Hasar Analitiği'),

        -- Acenteler & İş Ortaklıkları
        (N'Acenteler & İş Ortaklıkları', N'Kurumsal İletişim ve Reklam'),
        (N'Acenteler & İş Ortaklıkları', N'Müşteri Deneyim'),
        (N'Acenteler & İş Ortaklıkları', N'Acenteler ve İş Ortaklıkları Satış'),
        (N'Acenteler & İş Ortaklıkları', N'Dijital Pazarlama'),

        -- Oto Kaza, Oto Dışı Teknik ve Reasürans
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko Ürün ve Tarife'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko, Konut Ürün ve Tarife'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'İş Analizi ve Veri Yönetimi'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Sağlık Ürün & Tarife'),

        -- Oto Sorumluluk Sigortaları ve Hukuk
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Trafik Ürün & Tarife'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Rücu'),

        -- Mali İşler
        (N'Mali İşler', N'İç Denetim'),
        (N'Mali İşler', N'İç Kontrol'),
        (N'Mali İşler', N'İnsan Kaynakları'),
        (N'Mali İşler', N'Alacak Yönetimi'),
        (N'Mali İşler', N'Risk Yönetimi'),
        (N'Mali İşler', N'Bütçe ve Raporlama'),
        (N'Mali İşler', N'Mali ve İdari İşler')
    ) v(DeptName, UnitName)
)
INSERT INTO dbo.Units (DepartmentId, Name)
SELECT d.Id, s.UnitName
FROM src s
JOIN dbo.Departments d ON d.Name = s.DeptName
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Units u
    WHERE u.DepartmentId = d.Id AND u.Name = s.UnitName
);

-- CONCAT içine subquery konamaz (yalnızca skaler ifade); sayıyı önce değişkene al.
DECLARE @unitCount int = (SELECT COUNT(*) FROM dbo.Units);
PRINT CONCAT('OK: Birim seed tamam. Toplam birim: ', @unitCount);
GO

/* 3) Çalışan + Stajyer → UnitId (opsiyonel birim bağı) --------------------- */
-- Nullable: Yönetim gibi birimsiz departmanlar ve eski kayıtlar için boş kalabilir.
-- Doluysa seçilen birimin çalışanın/stajyerin departmanına ait olması iş kuralıdır (handler).
IF COL_LENGTH('dbo.Employees', 'UnitId') IS NULL
    ALTER TABLE dbo.Employees ADD UnitId int NULL
        CONSTRAINT FK_Employees_Units REFERENCES dbo.Units (Id);
GO

IF COL_LENGTH('dbo.Interns', 'UnitId') IS NULL
    ALTER TABLE dbo.Interns ADD UnitId int NULL
        CONSTRAINT FK_Interns_Units REFERENCES dbo.Units (Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_UnitId')
    CREATE INDEX IX_Employees_UnitId ON dbo.Employees (UnitId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Interns_UnitId')
    CREATE INDEX IX_Interns_UnitId ON dbo.Interns (UnitId);
GO

/* 4) Doğrulama: departman başına birim sayısı ------------------------------ */
SELECT d.Name AS Departman, COUNT(u.Id) AS BirimSayisi
FROM dbo.Departments d
LEFT JOIN dbo.Units u ON u.DepartmentId = d.Id
GROUP BY d.Name
ORDER BY d.Name;
GO
