# Veritabanı script'leri

Proje **Dapper** kullanıyor, EF Core yok — dolayısıyla migration mekanizması da yok.
Şemanın tek doğruluk kaynağı bu klasördür. Her şema değişikliği buraya yeni bir
numaralı dosya olarak eklenir ve git'te takip edilir.

## Çalıştırma

**Tek yapman gereken:**

```
05_full_setup.sql
```

Bu dosya 01–04'ün tamamını içerir. Hem sıfır veritabanında hem mevcut
veritabanında aynı sonucu verir; tekrar çalıştırılabilir (idempotent).
Engelleyici veri bulursa sessizce geçmez, durup ne yapılması gerektiğini yazar.

| Dosya | Durum |
|---|---|
| `05_full_setup.sql` | **Güncel kurulum dosyası — bunu çalıştır** |
| `01_schema.sql` | tarihsel: ilk şema |
| `02_seed_dev.sql` | tarihsel: ilk seed |
| `03_fixes.sql` | tarihsel: `NOT NULL` / `UNIQUE` düzeltmeleri |
| `04_hr_module.sql` | tarihsel: İK modülü genişletmesi |

01–04 geçmişi görmek için duruyor. Şema bundan sonra değişirse `06_...` diye
yeni bir dosya eklenir ve `05` de güncellenir.

## Tablolar

```
Departments ──┬── Employees ──┬── LeaveRequests ──┐
              │       │       ├── EmployeeNotes   │
              │       └── (ManagerId → Employees) │
              └── Interns ────┬── InternTasks     │
                              ├── InternNotes     │
                              └───────────────────┘  (LeaveRequests.InternId)
Users ── (Employees.UserId, Interns.UserId, not/görev yazarları, izin onaylayanı)
```

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
  biridir; `CK_LeaveRequests_Requester` kısıtı bunu garanti eder.
