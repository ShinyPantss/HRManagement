/* =============================================================================
   HRManagement — Organizasyon kadro seed'i                          (2026-07-24)

   KADRO MODELİ (piramit):
     - 1 adet GM (kıdem 1)              → Yönetim departmanında, birimsiz
     - Departman başına 1 GMY (kıdem 2) → departman seviyesinde (UnitId NULL);
                                          Yönetim hariç → 7 GMY (6 şema + Yazılım)
     - Birim başına 1 Müdür (kıdem 3)   → birim sayısı kadar müdür
     - Her birimde ekip                 → 1 Müdür Yrd (4), 1 Kıd. Uzman (5), 1 Uzman (6)

   ÇALIŞTIRMA SIRASI: 05_full_setup.sql → 11_units.sql → BU DOSYA.

   NASIL ÇALIŞIR (idempotent — tekrar çalıştırılabilir):
     0  Önceki sürümlerin modele uymayan seed kayıtları temizlenir
        (birim GM/GMY'leri, birimsiz departmanlara üretilmiş müdür+ekip).
     1  Departmanlar: org şemasındaki 6 departman + Yönetim yoksa eklenir.
     2  Birim seed'i (11_units ile aynı çiftler).
     3  Org şemasındaki GERÇEK yöneticiler (Ali Budak, Filiz Akın...) kendi
        birimlerine, şemadaki unvanlarına karşılık gelen kıdemle eklenir.
     4  GM: şirkette aktif GM yoksa Yönetim'e TEK kayıt eklenir.
     5  GMY: GMY'si olmayan her departmana (Yönetim hariç) BİR GMY eklenir.
     6  Her birimin (Müdür, Müdür Yrd, Kıd. Uzman, Uzman) boşlukları doldurulur.
     7  Yönetici zinciri: GMY'ler GM'e; diğerleri birim içindeki en yakın üste,
        birimde üst yoksa departman GMY'sine bağlanır.
     8  Örnek hesap talepleri (yalnızca İnsan Kaynakları birimi) + doğrulama.

   BİLİNÇLİ KARARLAR:
     - UserId hep NULL kalır. Hesap açma bu projede SQL'in işi değil:
       HR talep eder → Admin onaylar → ApproveAccountRequestCommandHandler
       Users satırını yaratıp kişiye bağlar. Rol de kıdemden türetilir
       (AccountRoleResolver: 1-3 → Yönetici, 4-6 → Çalışan). Seed bu akışı
       BESLER, atlamaz.
     - GM'in adı üretilmiştir: org şemasında GM'in adı görselde kesikti.
     - Var olan veri EZİLMEZ: koşul hep "yoksa ekle"; ManagerId yalnızca
       NULL ise doldurulur. Temizlik de yalnız seed'in KENDİ ürettiği,
       e-posta deseninden tanınan kayıtları siler.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE HRManagementDb;
GO

/* --- Ön koşul kontrolü ----------------------------------------------------- */
IF OBJECT_ID('dbo.Units', 'U') IS NULL
   OR COL_LENGTH('dbo.Employees', 'UnitId') IS NULL
   OR COL_LENGTH('dbo.Employees', 'Seniority') IS NULL
BEGIN
    RAISERROR('DURDURULDU: once db/05_full_setup.sql ve db/11_units.sql calistirilmali.', 16, 1);
    SET NOEXEC ON;   -- sonraki batch'ler calismasin
END
GO


/* =============================================================================
   0) TEMİZLİK — önceki sürümlerin modele uymayan seed kayıtları

   Eski sürümler her BİRİME GM/GMY, birimsiz departmanlara da müdür+ekip
   üretiyordu. Yeni modelde bunların yeri yok. Yalnızca seed'in ürettiği
   kayıtlar silinir — e-posta deseninden tanınırlar; elle girilen kayda
   dokunulmaz. Desenler:
     calisan.b{birim}.k1 / .k2   → birim GM'i ve birim GMY'si
     calisan.d{dept}.k1          → birimsiz departman GM'i
     calisan.d{dept}.k3..k6      → birimsiz departman müdür+ekibi
   (calisan.d{dept}.k2 KALIR — departman GMY'si yeni modelde de geçerli.)

   Silmeden önce seed'in kendi kurduğu referanslar çözülür (ManagerId,
   hesap talebi). Kullanıcı bu kayıtlara ELLE veri bağladıysa (izin, not...)
   FK bilerek durdurur — sessiz veri kaybındansa gürültülü hata.
   ============================================================================= */
DECLARE @eski TABLE (Id int PRIMARY KEY);

INSERT INTO @eski (Id)
SELECT Id FROM dbo.Employees
WHERE Email LIKE 'calisan.b%.k[12]@sirket.local'
   OR Email LIKE 'calisan.d%.k1@sirket.local'
   OR Email LIKE 'calisan.d%.k[3-6]@sirket.local';

UPDATE dbo.Employees SET ManagerId = NULL
WHERE ManagerId IN (SELECT Id FROM @eski);

DELETE FROM dbo.AccountRequests
WHERE EmployeeId IN (SELECT Id FROM @eski);

DELETE FROM dbo.Employees
WHERE Id IN (SELECT Id FROM @eski);

PRINT CONCAT('OK: ', @@ROWCOUNT, ' eski seed kaydi temizlendi');
GO


/* =============================================================================
   1) DEPARTMANLAR — org şemasındaki 6 departman + Yönetim (isimle, yoksa ekle)
      Yönetim = GM'in departmanı; birimi ve GMY'si olmaz (11_units notuyla uyumlu).
   ============================================================================= */
;WITH d(Name, Description) AS
(
    SELECT * FROM (VALUES
        (N'Yönetim',                                N'Genel Müdürlük'),
        (N'Bilgi Teknolojileri',                    N'IT — veri, sistem, uygulama ve entegrasyon'),
        (N'Hasar',                                  N'Hasar yönetimi ve operasyonu'),
        (N'Acenteler & İş Ortaklıkları',            N'Satış kanalları, pazarlama ve iletişim'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko/konut/sağlık ürün ve tarife'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk',    N'Trafik, rücu ve hukuk'),
        (N'Mali İşler',                             N'Finans, denetim, İK ve idari işler')
    ) v(Name, Description)
)
INSERT INTO dbo.Departments (Name, Description)
SELECT d.Name, d.Description
FROM d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Departments x WHERE x.Name = d.Name);

PRINT CONCAT('OK: ', @@ROWCOUNT, ' departman eklendi');
GO


/* =============================================================================
   2) BİRİMLER — 11_units.sql ile birebir aynı çiftler (idempotent)
   ============================================================================= */
;WITH src(DeptName, UnitName) AS
(
    SELECT * FROM (VALUES
        (N'Bilgi Teknolojileri', N'Veri Analitiği ve Mühendisliği'),
        (N'Bilgi Teknolojileri', N'Sistem ve Network'),
        (N'Bilgi Teknolojileri', N'Temel Sigortacılık Uygulamaları'),
        (N'Bilgi Teknolojileri', N'Proje ve Süreç Yönetimi'),
        (N'Bilgi Teknolojileri', N'Dijital Platformlar ve Entegrasyon'),
        (N'Bilgi Teknolojileri', N'Müşteri İletişim Merkezi ve Operasyon'),
        (N'Bilgi Teknolojileri', N'Kurumsal Mimariler'),
        (N'Hasar', N'Dijital Hasar'),
        (N'Hasar', N'Partner Yönetim'),
        (N'Hasar', N'Oto Ağır Hasar'),
        (N'Hasar', N'Hasar'),
        (N'Hasar', N'Hasar Lojistik'),
        (N'Hasar', N'Hasar Analitiği'),
        (N'Acenteler & İş Ortaklıkları', N'Kurumsal İletişim ve Reklam'),
        (N'Acenteler & İş Ortaklıkları', N'Müşteri Deneyim'),
        (N'Acenteler & İş Ortaklıkları', N'Acenteler ve İş Ortaklıkları Satış'),
        (N'Acenteler & İş Ortaklıkları', N'Dijital Pazarlama'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko Ürün ve Tarife'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko, Konut Ürün ve Tarife'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'İş Analizi ve Veri Yönetimi'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Sağlık Ürün & Tarife'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Trafik Ürün & Tarife'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Rücu'),
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
WHERE NOT EXISTS (SELECT 1 FROM dbo.Units u WHERE u.DepartmentId = d.Id AND u.Name = s.UnitName);

PRINT CONCAT('OK: ', @@ROWCOUNT, ' birim eklendi');
GO


/* =============================================================================
   3) ORG ŞEMASINDAKİ GERÇEK YÖNETİCİLER

   Kıdem, şemadaki unvana göre: Müdür/Direktör → 3 (11_units notu: Direktör =
   Müdür'e eşdeğer), Müdür Yardımcısı → 4, Uzman → 6. GMY / "bağlı olduğu kişi"
   satırları departman seviyesine (UnitName NULL) 2 olarak konur.
   Bir hücre doluysa ya da e-posta kullanımdaysa satır sessizce atlanır.
   ============================================================================= */
;WITH src(DeptName, UnitName, Kidem, FirstName, LastName, Email) AS
(
    SELECT * FROM (VALUES
        -- Bilgi Teknolojileri (hepsi Müdür)
        (N'Bilgi Teknolojileri', N'Veri Analitiği ve Mühendisliği',        3, N'Ali',          N'Budak',          'ali.budak@sirket.local'),
        (N'Bilgi Teknolojileri', N'Sistem ve Network',                     3, N'Aytaç',        N'Kaçak',          'aytac.kacak@sirket.local'),
        (N'Bilgi Teknolojileri', N'Temel Sigortacılık Uygulamaları',       3, N'Ekrem',        N'Büyüksarı',      'ekrem.buyuksari@sirket.local'),
        (N'Bilgi Teknolojileri', N'Proje ve Süreç Yönetimi',               3, N'Nalan',        N'Becerikli',      'nalan.becerikli@sirket.local'),
        (N'Bilgi Teknolojileri', N'Dijital Platformlar ve Entegrasyon',    3, N'Ömer',         N'Yalçın',         'omer.yalcin@sirket.local'),
        (N'Bilgi Teknolojileri', N'Müşteri İletişim Merkezi ve Operasyon', 3, N'Öznur',        N'Korkmaz',        'oznur.korkmaz@sirket.local'),
        (N'Bilgi Teknolojileri', N'Kurumsal Mimariler',                    3, N'Serkan',       N'Özdemir',        'serkan.ozdemir@sirket.local'),
        -- Hasar
        (N'Hasar', N'Dijital Hasar',   3, N'Ahmet',    N'Demirci',   'ahmet.demirci@sirket.local'),
        (N'Hasar', N'Partner Yönetim', 3, N'Gökhan',   N'Küçükece',  'gokhan.kucukece@sirket.local'),
        (N'Hasar', N'Oto Ağır Hasar',  3, N'Mihrigül', N'Aslan',     'mihrigul.aslan@sirket.local'),
        (N'Hasar', N'Hasar',           3, N'Onur',     N'Yılmaz',    'onur.yilmaz@sirket.local'),
        (N'Hasar', N'Hasar Lojistik',  3, N'Soner',    N'Yüce',      'soner.yuce@sirket.local'),
        (N'Hasar', N'Hasar Analitiği', 4, N'Turgay',   N'Topçu',     'turgay.topcu@sirket.local'),   -- şemada Müdür Yrd.
        -- Acenteler & İş Ortaklıkları (GMY: Burç Özer — departman seviyesi)
        (N'Acenteler & İş Ortaklıkları', NULL,                                   2, N'Burç',  N'Özer',          'burc.ozer@sirket.local'),
        (N'Acenteler & İş Ortaklıkları', N'Kurumsal İletişim ve Reklam',         3, N'Aslı',  N'Çolpan',        'asli.colpan@sirket.local'),
        (N'Acenteler & İş Ortaklıkları', N'Müşteri Deneyim',                     3, N'Gamze', N'Helvacıköylü',  'gamze.helvacikoylu@sirket.local'),
        (N'Acenteler & İş Ortaklıkları', N'Acenteler ve İş Ortaklıkları Satış',  3, N'Orçun', N'Bilgel',        'orcun.bilgel@sirket.local'),
        (N'Acenteler & İş Ortaklıkları', N'Dijital Pazarlama',                   3, N'Yasin', N'Alıcı',         'yasin.alici@sirket.local'),
        -- Oto Kaza, Oto Dışı Teknik ve Reasürans (bağlı: Duygu Bozkurt)
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', NULL,                            2, N'Duygu',       N'Bozkurt', 'duygu.bozkurt@sirket.local'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko Ürün ve Tarife',         4, N'Hülya Arife', N'Bilgin',  'hulya.bilgin@sirket.local'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko Ürün ve Tarife',         6, N'Aleyna',      N'Yeğin',   'aleyna.yegin@sirket.local'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Kasko, Konut Ürün ve Tarife',  4, N'Ceren',       N'Versan',  'ceren.versan@sirket.local'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'İş Analizi ve Veri Yönetimi',  3, N'Ömür',        N'Mavuş',   'omur.mavus@sirket.local'),
        (N'Oto Kaza, Oto Dışı Teknik ve Reasürans', N'Sağlık Ürün & Tarife',         3, N'Özgür',       N'Ergün',   'ozgur.ergun@sirket.local'),
        -- Oto Sorumluluk Sigortaları ve Hukuk (bağlı: Gamze Demirhan)
        (N'Oto Sorumluluk Sigortaları ve Hukuk', NULL,                     2, N'Gamze', N'Demirhan',        'gamze.demirhan@sirket.local'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Trafik Ürün & Tarife',  4, N'Alper', N'Temel',           'alper.temel@sirket.local'),
        (N'Oto Sorumluluk Sigortaları ve Hukuk', N'Rücu',                  4, N'Deniz', N'Paksoy Altınok',  'deniz.altinok@sirket.local'),
        -- Mali İşler (bağlı: Murat Doğu)
        (N'Mali İşler', NULL,                     2, N'Murat',  N'Doğu',     'murat.dogu@sirket.local'),
        (N'Mali İşler', N'İç Denetim',            3, N'Çağlar', N'Yaman',    'caglar.yaman@sirket.local'),
        (N'Mali İşler', N'İç Kontrol',            4, N'Esra',   N'Sapancı',  'esra.sapanci@sirket.local'),
        (N'Mali İşler', N'İnsan Kaynakları',      3, N'Filiz',  N'Akın',     'filiz.akin@sirket.local'),
        (N'Mali İşler', N'Alacak Yönetimi',       3, N'Günnur', N'Gemici',   'gunnur.gemici@sirket.local'),
        (N'Mali İşler', N'Risk Yönetimi',         4, N'Hazal',  N'Yeşil',    'hazal.yesil@sirket.local'),
        (N'Mali İşler', N'Bütçe ve Raporlama',    3, N'Nergis', N'Üçüncü',   'nergis.ucuncu@sirket.local'),
        (N'Mali İşler', N'Mali ve İdari İşler',   3, N'Semih',  N'Akpınar',  'semih.akpinar@sirket.local')
    ) v(DeptName, UnitName, Kidem, FirstName, LastName, Email)
)
INSERT INTO dbo.Employees
    (FirstName, LastName, DateOfBirth, DepartmentId, UnitId, HireDate, Email, IsActive, Seniority)
SELECT
    s.FirstName, s.LastName,
    DATEFROMPARTS(1968 + s.Kidem * 4, ((s.Kidem * 3) % 12) + 1, ((s.Kidem * 5) % 28) + 1),
    d.Id, u.Id,
    DATEFROMPARTS(2026 - (7 - s.Kidem) * 3, ((s.Kidem * 2) % 12) + 1, ((s.Kidem * 3) % 28) + 1),
    s.Email, 1, s.Kidem
FROM src s
JOIN dbo.Departments d ON d.Name = s.DeptName
LEFT JOIN dbo.Units u  ON s.UnitName IS NOT NULL AND u.DepartmentId = d.Id AND u.Name = s.UnitName
WHERE (s.UnitName IS NULL OR u.Id IS NOT NULL)                    -- birim bulunamadıysa atla
  AND NOT EXISTS (SELECT 1 FROM dbo.Employees e                   -- hücre zaten doluysa atla
                  WHERE e.IsActive = 1 AND e.Seniority = s.Kidem AND e.DepartmentId = d.Id
                    AND ((u.Id IS NULL AND e.UnitId IS NULL) OR e.UnitId = u.Id))
  AND NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.Email = s.Email);

PRINT CONCAT('OK: ', @@ROWCOUNT, ' org-semasi yoneticisi eklendi');
GO


/* =============================================================================
   4) GENEL MÜDÜR — şirkette TEK kayıt, Yönetim departmanında

   Şirket genelinde aktif bir GM varsa (nerede olursa olsun) eklenmez —
   "tam 1 GM" kuralının güvencesi budur. Adı üretilmiştir; org şemasında
   GM'in adı görselde kesik olduğu için gerçek isim bilinmiyor.
   ============================================================================= */
IF EXISTS (SELECT 1 FROM dbo.Employees WHERE Seniority = 1 AND IsActive = 1)
    PRINT 'ATLANDI: aktif GM zaten var';
ELSE
BEGIN
    INSERT INTO dbo.Employees
        (FirstName, LastName, DateOfBirth, DepartmentId, UnitId, HireDate, Email, IsActive, Seniority)
    SELECT N'Kenan', N'Başol',
           DATEFROMPARTS(1966, 5, 12),
           d.Id, NULL,
           DATEFROMPARTS(2005, 3, 1),
           'genel.mudur@sirket.local', 1, 1
    FROM dbo.Departments d
    WHERE d.Name = N'Yönetim'
      AND NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.Email = 'genel.mudur@sirket.local');

    PRINT CONCAT('OK: ', @@ROWCOUNT, ' GM eklendi (Yonetim)');
END
GO


/* =============================================================================
   5) DEPARTMAN GMY'LERİ — Yönetim HARİÇ her departmana tam 1 GMY

   Kontrol departman GENELİNDE yapılır (birimli-birimsiz fark etmez): departmanda
   aktif bir GMY varsa eklenmez. Org şemasından gelen 4 isimli GMY (3. bölüm)
   böylece korunur; kalan departmanlar (Bilgi Teknolojileri, Hasar, Yazılım...)
   üretilmiş isimle tamamlanır. GMY departman seviyesindedir: UnitId NULL.
   ============================================================================= */
;WITH ad(i, Name) AS
(
    SELECT * FROM (VALUES
        (0,N'Ahmet'),(1,N'Ayşe'),(2,N'Mehmet'),(3,N'Elif'),(4,N'Mustafa'),
        (5,N'Zeynep'),(6,N'Emre'),(7,N'Selin'),(8,N'Burak'),(9,N'Merve'),
        (10,N'Kerem'),(11,N'Derya'),(12,N'Volkan'),(13,N'Ebru'),(14,N'Tolga'),
        (15,N'Pınar'),(16,N'Cem'),(17,N'Gizem'),(18,N'Barış'),(19,N'Nazlı')
    ) v(i, Name)
),
soyad(i, Name) AS
(
    SELECT * FROM (VALUES
        (0,N'Yıldız'),(1,N'Kaya'),(2,N'Demir'),(3,N'Şahin'),(4,N'Çelik'),
        (5,N'Yıldırım'),(6,N'Öztürk'),(7,N'Aydın'),(8,N'Arslan'),(9,N'Doğan'),
        (10,N'Kılıç'),(11,N'Erdem'),(12,N'Çetin'),(13,N'Kara'),(14,N'Koç'),
        (15,N'Kurt'),(16,N'Özkan'),(17,N'Şimşek'),(18,N'Polat'),(19,N'Erdoğan')
    ) v(i, Name)
),
bosluk AS
(
    SELECT d.Id AS DeptId
    FROM dbo.Departments d
    WHERE d.Name <> N'Yönetim'
      AND NOT EXISTS (SELECT 1 FROM dbo.Employees e
                      WHERE e.DepartmentId = d.Id AND e.Seniority = 2 AND e.IsActive = 1)
)
INSERT INTO dbo.Employees
    (FirstName, LastName, DateOfBirth, DepartmentId, UnitId, HireDate, Email, IsActive, Seniority)
SELECT
    ad.Name, soyad.Name,
    DATEFROMPARTS(1976, ((b.DeptId + 2) % 12) + 1, ((b.DeptId * 3 + 2) % 28) + 1),
    b.DeptId, NULL,
    DATEFROMPARTS(2011, (b.DeptId % 12) + 1, ((b.DeptId * 2 + 2) % 28) + 1),
    CONCAT('calisan.d', b.DeptId, '.k2@sirket.local'),
    1, 2
FROM bosluk b
JOIN ad    ON ad.i    = (b.DeptId * 7 + 11) % 20
JOIN soyad ON soyad.i = (b.DeptId * 3 + 9)  % 20
WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e
                  WHERE e.Email = CONCAT('calisan.d', b.DeptId, '.k2@sirket.local'));

PRINT CONCAT('OK: ', @@ROWCOUNT, ' departman GMY''si eklendi');
GO


/* =============================================================================
   6) BİRİM KADROSU — her birimde 1 Müdür (3) + ekip: 4, 5, 6'dan birer kişi

   İsimler iki havuzdan deterministik seçilir (UnitId + kıdem modülü) —
   rastgelelik yok, script her ortamda aynı sonucu üretir. E-posta ise
   çakışmasın diye isimden değil Id'lerden kurulur: calisan.b{birim}.k{kıdem}@…

   Tarih mantığı: kıdem yükseldikçe işe giriş eskir (Müdür 2014'e, Uzman 2023'e
   düşer) — izin hakkı HireDate'ten hesaplandığı için bu, izin modülüne de
   anlamlı veri sağlar. Doğum yılı da kıdemle yaşlanır.
   ============================================================================= */
;WITH k(Kidem) AS
(
    SELECT v.n FROM (VALUES (3),(4),(5),(6)) v(n)   -- GM/GMY birim kadrosunda YOK (4. ve 5. bölüm)
),
ad(i, Name) AS
(
    SELECT * FROM (VALUES
        (0,N'Ahmet'),(1,N'Ayşe'),(2,N'Mehmet'),(3,N'Elif'),(4,N'Mustafa'),
        (5,N'Zeynep'),(6,N'Emre'),(7,N'Selin'),(8,N'Burak'),(9,N'Merve'),
        (10,N'Kerem'),(11,N'Derya'),(12,N'Volkan'),(13,N'Ebru'),(14,N'Tolga'),
        (15,N'Pınar'),(16,N'Cem'),(17,N'Gizem'),(18,N'Barış'),(19,N'Nazlı')
    ) v(i, Name)
),
soyad(i, Name) AS
(
    SELECT * FROM (VALUES
        (0,N'Yıldız'),(1,N'Kaya'),(2,N'Demir'),(3,N'Şahin'),(4,N'Çelik'),
        (5,N'Yıldırım'),(6,N'Öztürk'),(7,N'Aydın'),(8,N'Arslan'),(9,N'Doğan'),
        (10,N'Kılıç'),(11,N'Erdem'),(12,N'Çetin'),(13,N'Kara'),(14,N'Koç'),
        (15,N'Kurt'),(16,N'Özkan'),(17,N'Şimşek'),(18,N'Polat'),(19,N'Erdoğan')
    ) v(i, Name)
),
bosluk AS
(
    SELECT u.Id AS UnitId, u.DepartmentId, k.Kidem
    FROM dbo.Units u
    CROSS JOIN k
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e
                      WHERE e.UnitId = u.Id AND e.Seniority = k.Kidem AND e.IsActive = 1)
)
INSERT INTO dbo.Employees
    (FirstName, LastName, DateOfBirth, DepartmentId, UnitId, HireDate, Email, IsActive, Seniority)
SELECT
    ad.Name, soyad.Name,
    DATEFROMPARTS(1968 + b.Kidem * 4, ((b.UnitId + b.Kidem) % 12) + 1, ((b.UnitId * 3 + b.Kidem) % 28) + 1),
    b.DepartmentId, b.UnitId,
    DATEFROMPARTS(2026 - (7 - b.Kidem) * 3, (b.UnitId % 12) + 1, ((b.UnitId * 2 + b.Kidem) % 28) + 1),
    CONCAT('calisan.b', b.UnitId, '.k', b.Kidem, '@sirket.local'),
    1, b.Kidem
FROM bosluk b
JOIN ad    ON ad.i    = (b.UnitId * 7 + b.Kidem * 3)  % 20
JOIN soyad ON soyad.i = (b.UnitId * 3 + b.Kidem * 11) % 20
WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e
                  WHERE e.Email = CONCAT('calisan.b', b.UnitId, '.k', b.Kidem, '@sirket.local'));

PRINT CONCAT('OK: ', @@ROWCOUNT, ' birim kadrosu boslugu dolduruldu');
GO


/* =============================================================================
   7) YÖNETİCİ ZİNCİRİ

   İki kural, ikisi de yalnız ManagerId'si BOŞ olanları doldurur (elle atanmış
   yöneticilere dokunulmaz):

   a) GMY → GM: bütün GMY'ler (kıdem 2) şirketin tek GM'ine bağlanır —
      birim/departman şartı aranmaz, GM zaten tektir.

   b) Kıdem 3+ → önce kendi birimindeki en yakın üst (6→5→4→3); birimde üst
      yoksa departman seviyesindeki GMY (b.UnitId IS NULL koşulunun sebebi bu:
      Müdür'ün üstü birimde değil departmandadır).
   ============================================================================= */
DECLARE @gm int =
    (SELECT TOP (1) Id FROM dbo.Employees
     WHERE Seniority = 1 AND IsActive = 1
     ORDER BY Id);   -- birden fazla GM varsa (olmamalı) en eski kayıt alınır

IF @gm IS NULL
    PRINT 'ATLANDI: aktif GM yok — GMY''ler yoneticisiz kaldi.';
ELSE
BEGIN
    UPDATE dbo.Employees
    SET ManagerId = @gm
    WHERE IsActive = 1
      AND Seniority = 2
      AND ManagerId IS NULL
      AND Id <> @gm;

    PRINT CONCAT('OK: ', @@ROWCOUNT, ' GMY GM''ye baglandi');
END
GO

UPDATE e
SET e.ManagerId = ust.Id
FROM dbo.Employees e
CROSS APPLY
(
    SELECT TOP (1) b.Id
    FROM dbo.Employees b
    WHERE b.IsActive = 1
      AND b.Seniority < e.Seniority
      AND b.Seniority >= 2                          -- GM'e doğrudan bağlanılmaz
      AND b.DepartmentId = e.DepartmentId
      AND (b.UnitId = e.UnitId OR b.UnitId IS NULL) -- birimdeki üst; yoksa departman GMY'si
    ORDER BY b.Seniority DESC, b.Id
) ust
WHERE e.IsActive = 1
  AND e.ManagerId IS NULL
  AND e.Seniority > 2;   -- GMY'nin yöneticisi yukarıdaki (a) kuralıdır

PRINT CONCAT('OK: ', @@ROWCOUNT, ' calisana yonetici baglandi');
GO


/* =============================================================================
   8) ÖRNEK HESAP TALEPLERİ — yalnızca İnsan Kaynakları birimi (4 kişi)

   Amaç: Admin'in bekleyen-talepler ekranında iki rol türetimini de görmek
   (kıdem 1-3 → Yönetici, 4-6 → Çalışan; AccountRoleResolver ile aynı kural).
   Tüm çalışanlara açılırsa ekran yüzlerce taleple dolar; o yüzden tek birim.
   RequestedByUserId için ilk aktif Admin kullanılır (dev seed'deki "test").
   ============================================================================= */
DECLARE @requester int =
    (SELECT TOP (1) Id FROM dbo.Users WHERE Role = 1 AND IsActive = 1 ORDER BY Id);

IF @requester IS NULL
    PRINT 'ATLANDI: aktif Admin yok — hesap talebi acilmadi (once 02_seed_dev.sql).';
ELSE
BEGIN
    INSERT INTO dbo.AccountRequests (EmployeeId, RequestedByUserId, SuggestedRole, Note, Status)
    SELECT e.Id, @requester,
           CASE WHEN e.Seniority BETWEEN 1 AND 3 THEN 3 ELSE 4 END,   -- Manager / Employee
           N'Seed: İnsan Kaynakları birimi örnek talebi.', 1          -- Pending
    FROM dbo.Employees e
    JOIN dbo.Units u ON u.Id = e.UnitId
    WHERE u.Name = N'İnsan Kaynakları'
      AND e.IsActive = 1
      AND e.UserId IS NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.AccountRequests ar
                      WHERE ar.EmployeeId = e.Id AND ar.Status = 1);

    PRINT CONCAT('OK: ', @@ROWCOUNT, ' hesap talebi acildi (Insan Kaynaklari)');
END
GO


/* =============================================================================
   9) DOĞRULAMA

   a) Aktif GM sayısı — tam 1 beklenir.
   b) Kapsama matrisi — birim satırlarında Mudur..Uzman >= 1; departman
      satırında (birimsiz) GMY = 1 beklenir.
   c) Eksik hücre listesi — 0 satır beklenir; satır dönerse o hücre boş demektir.
   ============================================================================= */
SELECT COUNT(*) AS AktifGmSayisi   -- 1 beklenir
FROM dbo.Employees
WHERE Seniority = 1 AND IsActive = 1;
GO

SELECT d.Name AS Departman,
       ISNULL(u.Name, N'— (departman seviyesi)') AS Birim,
       COUNT(CASE WHEN e.Seniority = 1 THEN 1 END) AS GM,
       COUNT(CASE WHEN e.Seniority = 2 THEN 1 END) AS GMY,
       COUNT(CASE WHEN e.Seniority = 3 THEN 1 END) AS Mudur,
       COUNT(CASE WHEN e.Seniority = 4 THEN 1 END) AS MudurYrd,
       COUNT(CASE WHEN e.Seniority = 5 THEN 1 END) AS KidUzman,
       COUNT(CASE WHEN e.Seniority = 6 THEN 1 END) AS Uzman
FROM dbo.Employees e
JOIN dbo.Departments d ON d.Id = e.DepartmentId
LEFT JOIN dbo.Units u  ON u.Id = e.UnitId
WHERE e.IsActive = 1
GROUP BY d.Name, ISNULL(u.Name, N'— (departman seviyesi)')
ORDER BY Departman, Birim;
GO

;WITH k(Kidem) AS (SELECT v.n FROM (VALUES (3),(4),(5),(6)) v(n))
SELECT d.Name AS Departman, u.Name AS Birim, k.Kidem AS EksikKidem
FROM dbo.Units u
JOIN dbo.Departments d ON d.Id = u.DepartmentId
CROSS JOIN k
WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e
                  WHERE e.UnitId = u.Id AND e.Seniority = k.Kidem AND e.IsActive = 1)
UNION ALL
SELECT d.Name, N'— (departman seviyesi)', 2
FROM dbo.Departments d
WHERE d.Name <> N'Yönetim'
  AND NOT EXISTS (SELECT 1 FROM dbo.Employees e
                  WHERE e.DepartmentId = d.Id AND e.Seniority = 2 AND e.IsActive = 1);
GO

PRINT 'Seed tamam. AktifGmSayisi = 1 ve eksik-hucre sorgusu 0 satir olmalidir.';
GO

SET NOEXEC OFF;
GO
