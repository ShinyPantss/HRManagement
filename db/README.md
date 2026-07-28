# Veritabanı script'leri

Proje **Dapper** kullanıyor, EF Core yok — dolayısıyla migration mekanizması da yok.
Şemanın tek doğruluk kaynağı bu klasördür. Her şema değişikliği buraya yeni bir
numaralı dosya olarak eklenir ve git'te takip edilir.

Tüm script'ler **idempotent**'tir: hem sıfır veritabanında hem mevcut veritabanında
aynı sonucu verir, tekrar çalıştırılabilir. Engelleyici veri bulurlarsa sessizce
geçmez, durup ne yapılması gerektiğini yazarlar.

## Çalıştırma

Sıfırdan kurulum için **sırayla iki dosya zorunludur**:

```
1) 05_full_setup.sql     → tüm tablolar, kısıtlar, indeksler
2) 11_units.sql          → Units tablosu + Employees.UnitId / Interns.UnitId
```

> **Neden iki dosya?** `05_full_setup.sql` birleşik kurulum dosyasıdır ve
> 01–04, 06, 07, 08, 10, 14'ü içerir. Ancak **Units özelliği 05'ten sonra
> eklendiği için onun içinde yoktur.** Uygulama birim alanını kullandığından
> `11_units.sql` de çalıştırılmalıdır. (`05`'i güncelleyip tek dosyaya
> döndürmek açık bir iyileştirmedir.)

İsteğe bağlı olarak demo verisi:

```
3) 12_seed_org_kadro.sql          → örnek organizasyon kadrosu
4) 13_flatten_manager_chains.sql  → 12'nin kurduğu yönetici zincirlerini düzeltir (12 çalıştıysa ZORUNLU)
5) 15_backfill_employee_gender.sql → eski kayıtların cinsiyet alanını ada göre doldurur
```

## Dosyalar

| Dosya | Durum |
|---|---|
| `05_full_setup.sql` | **Ana kurulum dosyası — 1. sırada çalıştır** |
| `11_units.sql` | **Birimler — 2. sırada çalıştır** (05'e dahil değil) |
| `12_seed_org_kadro.sql` | opsiyonel: örnek kadro verisi |
| `13_flatten_manager_chains.sql` | veri onarımı: 12'nin yönetici merdivenini düzleştirir |
| `15_backfill_employee_gender.sql` | opsiyonel: cinsiyet backfill'i (ada göre sezgisel) |
| `01_schema.sql` | tarihsel: ilk şema — *05'e dahil* |
| `02_seed_dev.sql` | tarihsel: geliştirme seed'i — ⚠️ **canlıda çalıştırma** |
| `03_fixes.sql` | tarihsel: `NOT NULL` / `UNIQUE` düzeltmeleri — *05'e dahil* |
| `04_hr_module.sql` | tarihsel: İK modülü genişletmesi — *05'e dahil* |
| `06_account_requests.sql` | tarihsel: hesap talepleri — *05'e dahil* |
| `07_employee_seniority.sql` | tarihsel: kıdem kolonu — *05'e dahil* |
| `08_drop_position.sql` | tarihsel: Position kolonunun kaldırılması — *05'e dahil* |
| `10_leave_rules.sql` | tarihsel: iş günü + hastalık raporu — *05'e dahil* |
| `14_employee_gender.sql` | tarihsel: cinsiyet kolonu — *05'e dahil* |

`09_*` numarası kullanılmamıştır.

Tarihsel dosyalar geçmişi görmek için duruyor. Şema bundan sonra değişirse
`16_...` diye yeni bir dosya eklenir.

## Tablolar

```
Departments ──┬── Units ──────────────────────────┐
              ├── Employees ──┬── LeaveRequests ──┤
              │       │       ├── EmployeeNotes   │
              │       │       └── AccountRequests │
              │       └── (ManagerId → Employees) │
              └── Interns ────┬── InternTasks     │
                              ├── InternNotes     │
                              ├── AccountRequests │
                              └───────────────────┘  (LeaveRequests.InternId)

Units  ── (Employees.UnitId, Interns.UnitId — opsiyonel)
Users  ── (Employees.UserId, Interns.UserId, not/görev yazarları,
           izin onaylayanı/reddedeni, hesap talebi eden/işleyen)
```

Kolon kolon veri sözlüğü ve ER diyagramı: [`../docs/veri-modeli.md`](../docs/veri-modeli.md)

## Notlar

- **Tarihler UTC tutulur** (`SYSUTCDATETIME`). Sunucu saat dilimi değişirse
  karşılaştırmalar bozulmasın diye; kullanıcıya gösterilirken çevrilir.
- **`UpdatedAt` null bırakılır** kayıt hiç güncellenmediyse — `CreatedAt` ile
  aynı değere set etmek "hiç değişmedi" bilgisini yok ederdi.
- **İzin hakkı için ayrı tablo yoktur.** Hak, `HireDate`'ten kıdem hesaplanarak
  bulunur (İş Kanunu md. 53); `Employees.AnnualLeaveDays` yalnızca elle geçersiz
  kılma içindir. Kullanılan gün de onaylanmış + bekleyen taleplerden hesaplanır —
  hiçbir yerde saklanmaz, dolayısıyla iki kayıt arasında tutarsızlık oluşamaz.
- **`LeaveRequests`'te talep sahibi** `EmployeeId` veya `InternId`'den tam olarak
  biridir; `CK_LeaveRequests_Requester` kısıtı bunu garanti eder. Aynı kural
  `AccountRequests` için `CK_AccountRequests_Subject` ile geçerlidir.
- **Pozisyon kolonu yoktur** (`08_drop_position.sql`); pozisyon *Birim (yoksa
  Departman) + Kıdem* birleşiminden gösterim anında türetilir.
