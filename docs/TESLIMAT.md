# Teslimat Kontrol Listesi

Gereksinim dokümanı: `IK_Yonetim_Uygulamasi_Gereksinim_Dokumani.pdf`
Bu belge, dokümandaki her teslimat ve gereksinim maddesinin projede **nerede**
karşılandığını gösterir.

---

## 1. Teslimatlar (§7)

| # | Teslimat | Durum | Nerede |
|---|---|---|---|
| 1 | Kaynak kod (Git repo linki) | ✅ | <https://github.com/ShinyPantss/HRManagement> |
| 2 | README (kurulum & çalıştırma) | ✅ | [`README.md`](../README.md) |
| 3 | Mimari doküman (kısa): katmanlar, hangi projede ne var, örnek akış | ✅ | [`docs/mimari.md`](mimari.md) — örnek akış §4 |
| 4 | Veri modeli / ER diyagramı | ✅ | [`docs/veri-modeli.md`](veri-modeli.md) |
| 5 | En az 3–5 use case için unit test | ✅ | `tests/HRManagement.Application.Tests` — **12 test dosyası** |

### §7.5 — İsmen istenen test senaryoları

| Senaryo | Test |
|---|---|
| İzin başlangıç tarihi > bitiş tarihi olduğunda hata dönmesi | `Validators/CreateLeaveRequestCommandValidatorTests.cs` |
| Stajyer eklerken zorunlu alanlar dolu değilse validasyon hatası | `Validators/CreateInternCommandValidatorTests.cs` |

Ek olarak test edilenler: izin onay akışı durum geçişleri, izin hakkı hesabı,
çalışan görünürlük kuralı, mentorluk kuralı, yönetici atama kuralları,
stajyer görev durumu güncelleme, bekleyen onaylar sorgusu.

---

## 2. Teknoloji ve genel kurallar (§2)

| Gereksinim | Durum | Not |
|---|---|---|
| .NET 10 · ASP.NET Core (Web API + MVC UI) | ✅ | API + WebUI ayrı host |
| MSSQL · ORM: Dapper | ✅ | EF Core kullanılmadı |
| JWT tabanlı kimlik doğrulama | ✅ | + tarayıcı tarafında cookie |
| ASP.NET Core MVC / Razor · HTML/CSS | ✅ | Sunucuda render edilen Razor |
| Git · uzak repo | ✅ | GitHub |
| Dokümantasyon: README | ✅ | [`README.md`](../README.md) |
| Dokümantasyon: mimari/katman dokümanı | ✅ | [`docs/mimari.md`](mimari.md) |
| Dokümantasyon: veritabanı şeması | ✅ | [`docs/veri-modeli.md`](veri-modeli.md) + [`db/README.md`](../db/README.md) |

---

## 3. Mimari gereksinimler (§3)

| Gereksinim | Durum | Not |
|---|---|---|
| §3.1 En az 4 katman (Domain, Application, Infrastructure, Presentation) | ✅ | **5 proje** — Presentation ikiye ayrıldı: API + WebUI |
| Domain: entity'ler, hiçbir dış bağımlılık | ✅ | `Domain.csproj`'da tek NuGet paketi bile yok |
| Domain: iş kurallarına ait metotlar, anemik modelden kaçınma | ⚠️ | **Sapma** — bkz. §6 |
| Application: use-case'ler, DTO, Command/Query (CQRS), repository arayüzleri, validasyon | ✅ | MediatR + FluentValidation |
| Infrastructure: Dapper repository'leri, bağlantı ayarları | ✅ | |
| Presentation: controller'lar, Login/Register/Authorization uçları | ⚠️ | Register **yok** — bkz. §6 |
| §3.2 Bağımlılık kuralları | ✅ | `.csproj` referanslarıyla derleyici zorluyor |
| §3.2 İş kuralları Presentation'da değil Application/Domain'de | ✅ | Controller'lar ince |
| §3.3 Repository'ler Application'daki arayüzler üzerinden, DI ile bağlanır | ✅ | Composition root: `API/Program.cs` |

---

## 4. Fonksiyonel gereksinimler (§5)

| Gereksinim | Durum | Nerede |
|---|---|---|
| §4 Roller (Admin, HR, Yönetici, Çalışan/Stajyer) | ✅ | **5 rol** — Çalışan ve Stajyer ayrı |
| §5.1 Kullanıcı adı + şifre ile giriş | ✅ | `AuthController.Login` |
| §5.1 Şifreler hash'lenmiş | ✅ | BCrypt (`PasswordHasher`) |
| §5.1 Her kullanıcının bir rolü | ✅ | `Users.Role` |
| §5.1 Rol bazlı ekran erişimi | ✅ | `[Authorize(Roles=...)]` + global fallback policy |
| §5.1 Çalışan yalnızca kendi bilgilerini görür | ✅ | `EmployeeVisibility`, `GetMyEmployeeDetail` |
| §5.2 Çalışan listesi (ad, soyad, departman, pozisyon, e-posta, durum) | ✅ | Pozisyon **türetilir** — bkz. §6 |
| §5.2 Çalışan ekleme/güncelleme formu | ✅ | `Employees/Form` |
| §5.2 Kullanıcı hesabıyla ilişkilendirme (anında veya sonradan) | ✅ | `UserId` + `RequestLoginAccount` → hesap talebi |
| §5.2 Çalışan detayı: bilgiler + izin geçmişi + notlar | ✅ | `EmployeeDetailAssembler`, `EmployeeNotes` |
| §5.3.1 İzin talebi oluşturma (tür, tarihler, açıklama) | ✅ | + hastalık izni ve rapor alanı |
| §5.3.1 Kendi taleplerini listeleme (durum, tarih, tip) | ✅ | `LeaveRequests/Index` |
| §5.3.2 Bekleyen talepleri görme | ✅ | `GetPendingApprovals` — "Onay Bekleyenler" ekranı |
| §5.3.2 Onayla / Reddet (+ gerekçe) | ✅ | **İki aşamalı** onay akışı |
| §5.3.2 Raporlanabilir / filtrelenebilir liste | ✅ | "İzin Geçmişi" ekranı — durum filtresi + özet kutuları |
| §5.4 Stajyer listesi (ad, üniversite, bölüm, sınıf, tarihler, mentor) | ✅ | `Interns/Index` |
| §5.4 Stajyer detayı: bilgiler + görevler + mentor notları | ✅ | `InternTasks` ve `InternNotes` **ayrı tablolar** |
| §5.5 HR Dashboard (aktif çalışan, bekleyen izin, aktif stajyer sayısı) | ✅ | + cinsiyet dağılımı, departman kadrosu, izindekiler |
| §5.5 Yönetici Dashboard (ekip sayısı, ekipten gelen bekleyen izinler) | ✅ | `PersonalHomeViewModel` — "Ekibim" + "Onayımda bekleyen" |
| §5.5 Çalışan/Stajyer Dashboard (kalan izin, son talepler) | ✅ | `RemainingLeaveDays` + `RecentLeaveRequests`; stajyer için "Staj Panelim" |

---

## 5. Non-fonksiyonel gereksinimler (§6)

| Gereksinim | Durum | Not |
|---|---|---|
| §6.1 Clean Architecture'a uyum, doğru bağımlılık yönü | ✅ | |
| §6.1 İş mantığı controller'da değil | ✅ | |
| §6.1 Temiz ve anlamlı isimlendirme | ✅ | |
| §6.1 Yorumlar yalnızca gereken yerde | ✅ | Yorumlar "ne" değil "neden" anlatıyor |
| §6.2 Form girişlerinde temel validasyon | ✅ | WebUI (UX) + API (otorite) |
| §6.2 Application'da use-case bazlı validasyon | ✅ | FluentValidation + `ValidationBehavior` |
| §6.3 Sade ve anlaşılır hata mesajları | ✅ | Tek tip `BaseResponse` zarfı |
| §6.3 Global exception handling *(opsiyonel)* | ✅ | `GlobalExceptionHandler` + 2 ek mekanizma |
| §6.4 Şifreler hash'li | ✅ | BCrypt |
| §6.4 Login olmadan erişilememesi gereken uçlarda `[Authorize]` | ✅ | **Fallback policy** — uçlar kilitli doğar |
| §6.4 Rol bazlı `[Authorize(Roles=...)]` | ✅ | |

---

## 6. Bilinçli sapmalar

Aşağıdakiler eksik değil, **gerekçeli tercihlerdir**. Savunmada kendiliğinden
belirtilmelidir.

### 6.1. Anemik domain modeli (§3.1)

Gereksinim *"mümkün olduğunca anemik modelden kaçınma"* diyor; entity'ler veri
taşıyıcı olarak bırakıldı.

**Gerekçe:** Dapper doğrudan entity'ye eşleme yapar; bunun için parametresiz
kurucu ve yazılabilir property'ler gerekir. Zengin domain modeli ayrı bir
"persistence model" katmanı gerektirirdi. Ayrıca kuralların çoğu tek bir
entity'ye sığmıyor ("bu kişi bu izni onaylayabilir mi?" sorusu talebi, talep
sahibini, onaylayanın rolünü ve yönetici zincirini birlikte bilmeyi gerektirir).

**Riski nasıl kapattık:** Anemik modelin asıl zararı kuralların controller ve
servislere dağılıp izlenemez hâle gelmesidir. Burada her kural **adlandırılmış
ve test edilmiş** bir sınıfta: `LeaveApprovalGuard`, `EmployeeVisibility`,
`MentorshipGuard`, `UnitManagerResolver`, `LeaveEntitlement`. DDD terimiyle
bunlar *domain service*'tir.

### 6.2. Pozisyon alanı yok (§5.2)

`Employees.Position` kolonu kaldırıldı (`db/08_drop_position.sql`).

**Gerekçe:** Serbest metin pozisyon alanı tutarsız veri üretiyordu ("Uzman",
"uzman", "Yazılım Uzmanı"). Pozisyon artık *Birim (yoksa Departman) + Kıdem*
birleşiminden **türetiliyor** ve listede/detayda gösteriliyor. Gereksinimin
istediği **gösterim** karşılanıyor; farklı olan veri modeli.

### 6.3. Açık kayıt (Register) yok (§3.1)

Gereksinim Presentation katmanında *"Login / Register / Authorization uçları"*
diyor; projede yalnızca Login var.

**Gerekçe:** Bir İK sisteminde hesabı kişinin kendisi açamaz. Yerine
**`AccountRequest` onay akışı** kuruldu: İK çalışan/stajyer kaydı oluştururken
hesap talebi başlatır, Admin onaylar veya reddeder. Rol, kişinin kaydından
türetilir — kullanıcı kendi rolünü seçemez.

### 6.4. Rol sayısı (§4)

Gereksinim 4 rol sayıyor (Çalışan/Stajyer tek satırda); projede **5** rol var:
`Employee` ve `Intern` ayrıldı. Sebep: stajyerin izin hakkı (yıllık izin
biriktirmez), görev/mentor akışı ve ekranları çalışandan farklı.

---

## 7. Bilinen teknik borçlar

Teslimatı engellemeyen, bilinçli olarak ertelenmiş iyileştirmeler.
Ayrıntılı liste ve çözüm önerileri: teknoloji rehberi Bölüm 10.

| Eksik | Üretimde çözümü |
|---|---|
| Merkezî loglama yok (500'ler iz bırakmıyor) | `GlobalExceptionHandler`'a `ILogger` + `LoggingBehavior` |
| Transaction / Unit of Work yok | MediatR `TransactionBehavior` |
| Refresh token yok (2 saatte bir yeniden giriş) | Kısa ömürlü access + iptal edilebilir refresh token |
| Integration test yok (repository SQL'leri test edilmiyor) | `WebApplicationFactory` + Testcontainers/LocalDB |
| Listelerde sayfalama yok | `OFFSET / FETCH NEXT` |
| Eşzamanlılık kontrolü yok | `rowversion` ile iyimser kilitleme |

---

## 8. Teslim öncesi kontrol

- [ ] `dotnet build` temiz
- [ ] `dotnet test` tamamı yeşil
- [ ] Sıfır veritabanında kurulum denendi (`05_full_setup.sql` → `11_units.sql`)
- [ ] `README.md` adımlarıyla temiz bir makinede uygulama ayağa kalkıyor
- [ ] user-secrets değerleri **repoda değil** (`git grep` ile şifre/anahtar araması yapıldı)
- [ ] `db/02_seed_dev.sql` canlı ortamda çalıştırılmadı
- [ ] Uzak repo güncel (`git push`)
- [ ] `docs/` altındaki dört doküman repoda mevcut
