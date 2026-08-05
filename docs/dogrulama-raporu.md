# Bağımsız Doğrulama Raporu

Doğrulayan: `dogrulayici` (bağımsız üye — `kodlayici`'nın raporuna değil koda bakar)
Taban çizgisi commit'i: `c249aaa` (açıkların tespit edildiği hâl)
Doğrulama sırasındaki HEAD: `cbe9cbf`

---

## Bölüm 1 — Taban çizgisi (Aşama 1)

`kodlayici` çalışırken çıkarıldı. `git diff --stat c249aaa..cbe9cbf` sonucu: yalnızca
`src/HRManagement.API/Controllers/InternsController.cs` (+5 satır) ve `docs/mimari.md`
silinmesi. Yani aşağıdaki 10 açığın **9'u bu anda hâlâ taban çizgisi hâlindedir**;
sadece #2'ye dokunulmuştur.

### Taban test sayımı

```
Başarılı!  - Başarısız: 0, Başarılı: 110, Atlanan: 0, Toplam: 110, Süre: 408 ms
```

110 test yeşil — teyit edildi.

**Ortam notu:** API (PID 28268) ve WebUI (PID 19612) çalışır durumda olduğu için
çözümün tamamına `dotnet build` atıldığında MSB3021/MSB3027 **dosya kilidi** hatası
alınıyor — bunlar derleme hatası DEĞİL. `dotnet test` etkilenmiyor (yalnızca
Domain + Application + Tests derleniyor). Nihai doğrulamada API/WebUI'nin de
derlendiğinden emin olmak için uygulamaların durdurulması gerekir.

---

### K1 — Guard denetlenen metni değil ham metni çalıştırıyor

**Kırıklık nerede:**
- `SqlReadOnlyGuard.cs:44` → denetim `StripComments(sql)` çıktısı (`stripped`) üzerinde yapılır.
- `AskAssistantQueryHandler.cs:146` → çalıştırılan `sql`, yani **ham metin**.
- `SqlReadOnlyGuard.cs:96-100` → `StripComments` string sabitlerinden habersiz düz regex:
  `/\*.*?\*/` ve `--[^\r\n]*`.

**Sonuç:** T-SQL'in string sabiti saydığı bir `--` veya `/*`, guard tarafından yorum
sanılır ve arkasındaki gerçek ifade denetimden **silinir**; ham metinde ise o ifade
yerinde durur ve çalışır. İki ayrı metin üzerinde çalışan iki motor = klasik parser
uyumsuzluğu (parser differential).

**Somut atlatma (taban çizgisinde geçerli):**

| Girdi | Guard'ın gördüğü (`stripped`) | SQL Server'ın gördüğü |
|---|---|---|
| `SELECT '--' AS x, 1; DROP TABLE Employees` | `SELECT '` → tek ifade, SELECT ile başlıyor, yasak kelime yok → **KABUL** | `'--'` bir string; `DROP TABLE Employees` **çalışır** |
| `SELECT '/*' AS x; DROP TABLE Employees; SELECT '*/'` | `SELECT ' '` → **KABUL** | `'/*'` ve `'*/'` string; ortadaki DROP **çalışır** |

**Kapanma ölçütü (üçü de gerekli):**
1. `_sqlQueryRunner.RunReadOnlyAsync` çağrısına **denetimden geçen metnin aynısı** gitmeli
   (ya sanitize edilmiş metin çalıştırılmalı, ya da denetim ham metin üzerinde string-farkında
   yapılmalı). "Denetlenen metin ≠ çalışan metin" durumu tamamen ortadan kalkmalı.
2. Yorum ayıklaması **string sabitlerini tanımalı**: `'...'`, `''` kaçışı, `N'...'` öneki,
   `[...]` köşeli tanımlayıcı içinde geçen `--` / `/*` yorum sayılmamalı.
3. Yukarıdaki iki satırın ikisi de `IsReadOnly` tarafından **reddedilmeli**.

**Yanlış pozitif ölçütü:** `SELECT TOP 10 CreatedAt FROM Employees`, `WITH x AS (...) SELECT ...`,
`SELECT * FROM E WHERE Name LIKE '%-%'`, sondaki tek `;` — hâlâ **kabul** edilmeli.

---

### #2 — Stajyer IDOR (`GET /api/interns/{id}`)

**Taban çizgisi (`c249aaa`):** `GetById` üzerinde hiç `[Authorize(Roles=...)]` yok →
global fallback policy sadece "giriş yapmış olmak" istiyor. Yani `Employee` rolündeki
herhangi bir kullanıcı istediği stajyerin e-posta/üniversite/mentor bilgisini okuyabiliyor.
`GetInternByIdQueryHandler.cs:20` aktör almıyor, ilişki kontrolü yok.

**Kapanma ölçütü:** Uç, listeleme ucuyla (satır 38) en az aynı sıkılıkta olmalı; ayrıca
`Intern` rolü gibi geniş bir rol bırakılıyorsa bunun **bilinçli** olduğu ve stajyerin
başka stajyerin kaydını görmesinin kabul edildiği gösterilmeli. WebUI'da bu ucu çağıran
`InternsController.Edit` (HR,Admin) ve `AccountRequestsController` (Admin) kırılmamalı.

---

### Y1 — `NationalId` liste ve `GetById` yolunda kırpılmıyor

**Kırıklık nerede:**
- `EmployeeMapping.cs:17` → `NationalId = employee.NationalId` **koşulsuz**.
- `GetAllEmployeesQueryHandler.cs:52` ve `GetEmployeeByIdQueryHandler.cs:26` bu mapping'i
  olduğu gibi kullanır; ikisinde de kırpma yok.
- Karşı örnek: `EmployeeDetailAssembler.cs:82` → `visibility.CanSeeNationalId ? ... : null`,
  ve `:133` → `CanSeeNationalId: requester?.Role == Role.HR` (yalnızca HR, Admin bile hayır).

**Sonuç:** Detay yolu T.C. kimliği yalnızca HR'a verirken, liste ve `GetById` yolu onu
görebilen **herkese** (Manager kendi ekibi için, Employee kendisi ve gerekirse başkaları
için) veriyor.

**Kapanma ölçütü:** Üç yolun (liste, GetById, detay) kuralı **AYNI** olmalı ve tek yerde
tanımlı olmalı. İki farklı `CanSeeNationalId` tanımı doğarsa bu yeni bir tutarsızlıktır.

---

### #4 — Loglama yok

**Kırıklık nerede:** `GlobalExceptionHandler.cs:30` → `_ => (500, "Beklenmeyen bir hata oluştu.")`.
Exception nesnesi hiçbir yere yazılmadan **yutulur**. `grep -rn "ILogger" src/` → **0 sonuç**;
projenin tamamında tek bir logger yok.

**Kapanma ölçütü:** `GlobalExceptionHandler` bir `ILogger` alıp beklenmeyen exception'ı
stack trace'iyle birlikte `LogError` etmeli. Kullanıcıya dönen mesaj **değişmemeli**
(iç detay sızmasın) — hem loglayıp hem mesajı zenginleştirmek yeni bir açıktır.

---

### H-1 — Mentorsuz stajyerin talebi kilitleniyor

**Kırıklık nerede:**
- `CreateLeaveRequestCommandHandler.cs:82-83` → `skipManagerStage` yalnızca
  `Sick` **veya** `employee is not null && employee.ManagerId is null` hâlinde true.
  Stajyer dalı hiç düşünülmemiş → mentoru olmayan stajyerin talebi `Pending` doğar.
- `GetPendingApprovalsQueryHandler.cs:59-63` → `Pending` dalında stajyer için koşul
  `c.MentorId is int mid && (...)`. `MentorId` null ise koşul false.

**Sonuç:** Talep `Pending`'de asılı kalır; **yalnızca Admin** (`isAdmin` kısa devresi) onu
görebilir, HR göremez. HR ekranında hiç listelenmez.

**Kapanma ölçütü (iki kod yolu birden):**
1. **Yazma yolu:** mentoru olmayan stajyerin talebi doğrudan `PendingHr` doğmalı.
2. **Okuma yolu:** `GetPendingApprovalsQueryHandler` — eski, hâlihazırda DB'de `Pending`
   durumda asılı duran mentorsuz stajyer talepleri de HR'a görünmeli (ya da bunun
   bilinçli olarak kapsam dışı bırakıldığı yazılmalı). Yalnızca yazma yolunu düzeltmek
   "yeni kayıtlar düzeldi, eskiler hâlâ asılı" demektir.

---

### R-1 — `EnsureBalanceStillSufficientAsync` yalnızca `Pending` dalında

**Kırıklık nerede:** `ApproveLeaveRequestCommandHandler.cs:55-56` — bakiye yeniden denetimi
`case LeaveStatus.Pending:` içinde. `case LeaveStatus.PendingHr:` (satır 68-72) hiç denetim
yapmadan `Approved` yazar.

**Sonuç:** İki aşamalı akışta İK onayı (son aşama, asıl bağlayıcı olan) bakiyeyi hiç kontrol
etmez. Ayrıca `skipManagerStage` ile doğrudan `PendingHr` doğan talepler (hastalık, GM)
hiçbir aşamada ikinci kontrolü görmez.

**Kapanma ölçütü:**
1. Kontrol her iki dalda da çalışmalı (veya `switch`'ten önce ortak yere alınmalı).
2. **Regresyon riski:** ortak yere alınırsa `Pending` dalında **çift** çalışmamalı.
3. **Regresyon riski:** `EmployeeId` null olan (stajyer) talepte `NullReferenceException` /
   "çalışan kaydı bulunamadı" hatası vermemeli — mevcut korumada `is int employeeId`
   pattern'i bunu sağlıyor, taşınırken kaybolmamalı.
4. `Type == Annual` şartı korunmalı (Sick/Unpaid haktan düşmez).

---

### H-2 — `LeaveEntitlement.cs:18` yorumu kodla çelişiyor

**Kırıklık nerede:**
- Yorum (satır 18): *"1–5. yıl 14 gün, **6–15. yıl 20 gün**, 15+ yıl 26 gün"*
- Kod (`GrantForYear`, satır 39-45): `<= 5 => 14`, `< 15 => 20`, `_ => 26`
  → 20 gün aralığı **6–14**, 26 gün **15. yıldan itibaren**.

15. yıl için yorum "20" der, kod "26" verir. Kod İş Kanunu md. 53'e uygun (15 yıl ve
üzeri 26 gün); **yanlış olan yorumdur**.

**Kapanma ölçütü:** Yorum düzeltilmeli (`6–14. yıl 20 gün, 15. yıl ve sonrası 26 gün`),
**kod değişmemeli**. Kodun yoruma uydurulması gerçek bir hata üretir — bu regresyonu
özellikle kontrol edeceğim.

---

### H-5 — `GetOrganizationQueryHandler.cs:74` `DateTime.Today`

**Kırıklık nerede:** `IsActive = i.EndDate.Date >= DateTime.Today` — sunucunun **yerel**
saatine bağlı. Projenin geri kalanı UTC kullanır (`CreateLeaveRequestCommandHandler.cs:46,112`,
`ApproveLeaveRequestCommandHandler.cs:47,106`, `GetAllEmployeesQueryHandler.cs:47` — hepsi
`DateTime.UtcNow`). Tek istisna bu satır.

**Kapanma ölçütü:** `DateTime.UtcNow.Date` olmalı; başka `DateTime.Today`/`DateTime.Now`
kalmamalı (`grep` ile tarayacağım).

---

### H-3 — `NationalId` doğrulanmıyor, DB'de UNIQUE yok

**Kırıklık nerede:**
- `CreateEmployeeCommandValidator.cs` (67 satırın tamamı) → `NationalId` için **tek kural yok**.
- `UpdateEmployeeCommandValidator.cs` → aynı şekilde yok.
- `db/01_schema.sql:67` → `NationalId nvarchar(11) NULL`, kısıt yok. Aynı dosyada
  `UQ_Users_Username`, `UQ_Users_Email`, `UQ_Employees_Email` **var** — yani UNIQUE
  koyma alışkanlığı mevcut, `NationalId` atlanmış.
- `CreateEmployeeCommandHandler.cs:76` ve `UpdateEmployeeCommandHandler.cs:84` →
  `NationalId` hiç `Trim()` bile edilmeden yazılıyor (e-posta `Trim()` ediliyor, satır 41).

**Kapanma ölçütü:**
1. Validator'da: doluysa **tam 11 hane, yalnızca rakam** (ve tercihen T.C. kimlik algoritması).
2. Benzersizlik: DB'de UNIQUE **ve** uygulama katmanında önden kontrol
   (e-postadaki `GetByEmailAsync` deseniyle simetrik → 500 yerine anlaşılır 400).
3. **Kritik ölçüt:** DB script'i çalıştırılmamış bir ortamda bile uygulama katmanı doğru
   davranmalı. Yalnızca script yazıp uygulama katmanına dokunmamak açığı kapatmaz.
4. `NULL` hâlâ serbest kalmalı (alan opsiyonel) — UNIQUE eklenirse birden fazla NULL'a
   izin veren filtreli index gerekir, aksi hâlde ikinci NULL kaydı patlar (regresyon).

---

### H-4 — Stajyer e-postasında benzersizlik yok

**Kırıklık nerede:**
- `db/01_schema.sql:97` → `Interns.Email nvarchar(100) NOT NULL`, **UNIQUE yok**
  (aynı dosyada `UQ_Employees_Email` var → asimetri).
- `CreateInternCommandHandler.cs` (65 satırın tamamı) → e-posta benzersizlik kontrolü yok.
  Karşılaştırma: `CreateEmployeeCommandHandler.cs:55-56` bu kontrolü **yapıyor**.
- `UpdateInternCommandHandler` → aynı eksik.

**Sonuç:** Aynı e-postayla iki stajyer açılabilir. E-posta kimliğe bağlanan bir alan
olduğu için (hesap talebi akışı) bu ileride yanlış hesap eşleşmesi üretir.

**Kapanma ölçütü:**
1. `Interns.Email` üzerinde UNIQUE (DB) **ve** handler'da önden kontrol.
2. **Kapsam sorusu:** benzersizlik yalnızca `Interns` içinde mi, `Employees`+`Users`
   ile çapraz mı? Bir stajyer, var olan bir çalışanın e-postasıyla açılabiliyorsa açık
   yalnızca yarı kapanmıştır — bunu ayrıca kontrol edeceğim.
3. Update yolunda **kendi kaydını** çakışma sayıp kilitlememeli (regresyon).

---

## Bölüm 2 — Doğrulama sonuçları (Aşama 2)

Yöntem: `kodlayici`'nın raporu okundu ama **kanıt olarak kabul edilmedi**; her madde
için kodun kendisi okundu, ayrıca repo dışında bağımsız bir prob projesi ve
`tests/HRManagement.Application.Tests/Verification/GapClosureTests.cs` yazıldı.

| # | Açık | Sonuç |
|---|---|---|
| K1 | Guard ham metni çalıştırıyor | **KAPANDI** + ayrı bir **YENİ SORUN** (bkz. Bölüm 3) |
| 2 | Stajyer IDOR | **KAPANDI** (kodlayıcının raporu bu maddede yanlış) |
| Y1 | NationalId liste sızıntısı | **KAPANDI** (1 artık sızıntı, Bölüm 4) |
| 4 | Loglama | **KAPANDI** |
| H-1 | Mentorsuz stajyer kilidi | **KISMEN** — yazma yolu kapandı, mevcut veri açıkta |
| R-1 | Bakiye yeniden denetimi | **KAPANDI** |
| H-2 | Yorum/kod çelişkisi | **KAPANDI** |
| H-5 | `DateTime.Today` | **KAPANDI** (Application); WebUI'da kalan var, Bölüm 5 |
| H-3 | NationalId doğrulama/benzersizlik | **KAPANDI** (uygulama katmanı); DB script'i çalıştırılmadı |
| H-4 | Stajyer e-posta benzersizliği | **KAPANDI** (uygulama katmanı); kapsam sınırı var, Bölüm 4 |

### K1 — KAPANDI (özü), ayrıca yeni bir sorun bulundu

Bölüm 1'deki üç kapanma ölçütünün üçü de sağlanıyor:

1. **"Denetlenen metin = çalışan metin" invaryantı kuruldu.**
   `AskAssistantQueryHandler.cs:150` artık `TryNormalize`'ın döndürdüğü `safeSql`'i
   çalıştırıyor, ham `sql`'i değil; `executedQueries` listesine de o giriyor
   (satır 145) — kayıt gerçeği yansıtıyor.
2. **Yorum ayıklaması string sabitlerini tanıyor.** Regex tamamen kalktı; yerine
   `TryScan` (`SqlReadOnlyGuard.cs:125`) tek geçişli, tırnak farkındalıklı bir
   tarayıcı. `''` kaçışı, `[...]` / `"..."` tanımlayıcılar, iç içe `/* */` derinlik
   sayımı ve kapanmamış tırnak/yorumun REDDİ dahil.
   Tasarım doğru: `executable` (çalışacak) ile `inspected` (denetlenecek) tek
   geçişte üretiliyor ve yalnızca *veri* kısmında ayrışıyorlar, yapıda değil.
3. **Bölüm 1'de yazdığım iki atlatma da artık reddediliyor** (`Birden fazla ifade
   çalıştırılamaz`).

**Bağımsız kanıt:** `GapClosureTests.cs` içinde parser-differential sınıfından
**17 saldırı vektörü** yazdım (kodlayıcının test dosyasından bağımsız, farklı
vektörler: `N'...'` öneki, `[a]]--]` kaçışlı köşeli tanımlayıcı, `"--"` çift
tırnaklı tanımlayıcı, `\r` satır sonu, BOM, sekme, kapanmamış tırnak/yorum...).
**17/17'si engellendi.** Ayrıca 3 "sözleşme" testi: kabul edilen sorguda metin
sabitinin içeriği bozulmadan `safeSql`'e geçiyor.

**Yanlış pozitif kontrolü: 14 meşru sorgu, 14'ü de kabul edildi** — guard aşırı
sıkılaştırılmamış. `CreatedAt` / `UpdatedAt` kolonları, `WITH` CTE, `LIKE '%-%'`,
`'O''Brien'`, `N'Mücahit'`, `[First Name]`, `Grade - 1`, `UNION ALL`,
`OFFSET/FETCH`, gerçek yorumlar, sondaki `;` — hepsi geçiyor. Asistan
kullanılamaz hâle gelmemiş.

Buna karşılık K1'den BAĞIMSIZ, daha önce raporlanmamış bir zafiyet buldum —
Bölüm 3'te.

### 2 — Stajyer IDOR: KAPANDI (kodlayıcının raporu bu maddede yanlış)

`kodlayici` raporunda "zaten kapalıymış, DOKUNMADIM, uç `HR,Admin,Intern`" diyor.
**Kod bunu doğrulamıyor.** Çalışma ağacındaki `InternsController.cs:57`:

```csharp
[Authorize(Roles = "HR,Admin")]
[HttpGet("{id:int}")]
```

Yani uç HEAD'deki (`fb03b87`) `HR,Admin,Intern` hâlinden `HR,Admin`'e
**daraltılmış** ve gerekçesi yorumda yazılmış. İstenen budur; kodlayıcının özet
metni güncel değil. *(Bu tam olarak "rapora değil koda bak" ilkesinin karşılığı.)*

**Regresyon kontrolü — kırılan çağıran yok:**
- `WebUI/InternsController.cs:92` (`Edit`) → `[Authorize(Roles = "HR,Admin")]` ✔
- `WebUI/AccountRequestsController.cs:106` → Admin action'ı ✔
- `API/InternsController.Create` içindeki `CreatedAtAction(nameof(GetById), ...)`
  yalnızca URL üretir, yetki değerlendirmesi yapmaz ✔

`GetAll` ucundaki `Intern` rolü **kullanıcı kararı beklediği için KAPANMADI
sayılmadı** — ayrı madde olarak Bölüm 5'te.

### Y1 — KAPANDI

Kapanma ölçütüm "üç yolun kuralı AYNI ve TEK yerde olmalı" idi; sağlanıyor.
Yeni `EmployeeFieldVisibility.CanSeeNationalId(User?)` tek kaynak:

- `EmployeeDetailAssembler.cs:134` (detay)
- `GetAllEmployeesQueryHandler.cs:43` (liste)
- `GetEmployeeByIdQueryHandler.cs:39` (tekil)

**Lead'in özellikle sorduğu kontrol: hiçbir çağıran `true` sabitlemiyor** —
üçü de yardımcıyı çağırıyor. `EmployeeMapping.ToDto`'nun parametresi varsayılansız
ZORUNLU olduğu için derleyici yeni çağıranı da karar vermeye zorluyor; bu, kuralın
gelecekte sessizce atlanmasını yapısal olarak engelliyor. İyi bir tercih.

`GetEmployeeByIdQueryHandler`'a `IUserRepository` eklenmiş (rol DB'den okunuyor,
JWT claim'inden değil) — detay yoluyla tutarlı.

### 4 — Loglama: KAPANDI

`GlobalExceptionHandler` `ILogger<GlobalExceptionHandler>` alıyor; **yalnızca 500
dalında** `_logger.LogError(exception, "İşlenmemiş hata: {Method} {Path}", ...)`.
Ölçütlerim karşılanıyor: exception nesnesi **ilk parametre** (yığın izi ve iç
exception'lar ancak böyle loglanır) ve **istemciye giden mesaj değişmemiş**
("Beklenmeyen bir hata oluştu.") — iç detay sızmıyor.
400'lerin loglanmaması bilinçli ve doğru: onlar normal akış.

DI: `DependencyInjection.cs:24` `AddExceptionHandler<GlobalExceptionHandler>()`
(singleton) — `ILogger<T>` singleton olarak kayıtlı, çözülür. *Not: derleme ile
doğruladım, API'yi çalıştırıp çalışma anında denemedim.*

### H-1 — KISMEN

**Yazma yolu KAPANDI:** `CreateLeaveRequestCommandHandler.cs:88` →
`|| (intern is not null && intern.MentorId is null)`. Mentorsuz stajyerin YENİ
talebi `PendingHr` doğuyor, `GetPendingApprovalsQueryHandler`'ın `PendingHr` dalı
`(isHr || isAdmin)` dediği için İK listesinde görünüyor.

**Okuma yolu DÜZELTİLMEDİ.** `git diff` ile doğruladım:
`Features/LeaveRequests/Queries/GetPendingApprovals/` **hiç değişmemiş**. Sonuç:
düzeltmeden ÖNCE açılmış, hâlâ `Pending` durumda duran mentorsuz stajyer
talepleri İK'ya görünmüyor.

Ölçülülük notu: bunlar **kilitli değil**. `GetPendingApprovalsQueryHandler.cs:60`
`isAdmin` kısa devresi ve `LeaveApprovalGuard.EnsureManagerStageAsync`'teki
`if (actor.Role == Role.Admin) return;` sayesinde **Admin** bu talepleri hem
görüyor hem işleyebiliyor. Yani kalan iş "İK göremiyor, Admin görebiliyor" —
veri düzeltmesi (backfill) ya da okuma yolunda ek bir dal gerekiyor.

### R-1 — KAPANDI

Bölüm 1'deki dört ölçütün dördü de sağlanıyor:
1. Denetim `switch`'in **dışına** alındı (`ApproveLeaveRequestCommandHandler.cs:54`),
   böylece `PendingHr` dalı ve yönetici aşamasını atlayan talepler de kapsanıyor.
2. **Çift çalışmıyor** — kodda tek çağrı var, `Pending` dalındaki eski kopya silinmiş.
3. **Stajyerde patlamıyor** — `leaveRequest.EmployeeId is int employeeId` pattern'i
   korunmuş; `EmployeeId` null olan stajyer talebi denetime hiç girmiyor.
4. `Type == LeaveType.Annual` şartı korunmuş.

### H-2 — KAPANDI

`git diff` tam olarak **tek satır** gösteriyor ve o satır bir yorum:
`6–15. yıl 20 gün, 15+ yıl 26 gün` → `6–14. yıl 20 gün, 15. yıl ve sonrası 26 gün`.
**Kod değişmemiş** — Bölüm 1'de "asıl regresyon riski" diye işaretlediğim şey
(kodun yanlış yoruma uydurulması) olmamış. `GapClosureTests.cs`'e sınır yıllarını
kilitleyen 6 test yazdım (1/5/6/14/15/30 → 14/14/20/20/26/26), **6'sı da yeşil**.

### H-5 — KAPANDI

`GetOrganizationQueryHandler.cs:77` → `DateTime.UtcNow.Date`. Application katmanını
taradım, başka `DateTime.Today` / `DateTime.Now` **kalmadı**. (WebUI'da kalanlar
var — Bölüm 5.)

### H-3 — KAPANDI (uygulama katmanı), DB script'i çalıştırılmadı

- **Format:** Create + Update validator'larında `Length(11)` + `Matches("^[0-9]{11}$")`,
  `.When(!IsNullOrWhiteSpace)` ile — alan opsiyonel kalmış (mevcut davranış korunmuş).
- **Benzersizlik:** `IEmployeeRepository.GetByNationalIdAsync` + Dapper implementasyonu;
  Create'te ön kontrol, Update'te "kendi kaydı hariç" (`byNationalId.Id != employee.Id`)
  — e-postadaki desenin birebir aynısı.
- **Bölüm 1'deki kritik ölçüt sağlandı:** uygulama katmanı, `db/20_...sql`
  çalıştırılmadan da doğru davranıyor.
- **NULL ölçütü sağlandı:** handler boş T.C.'yi `null` yazıyor ve script
  `UNIQUE constraint` değil **filtered index** (`WHERE NationalId IS NOT NULL`)
  kuruyor — Bölüm 1'de "ikinci NULL kaydı patlar" diye işaretlediğim regresyon
  bilinçli olarak önlenmiş. Script idempotent, engelleyici veri varsa sessizce
  geçmeyip `RAISERROR` ile duruyor. Doğru yazılmış.
- T.C. kimlik **algoritma** doğrulaması yok (yalnızca format) — Bölüm 1'de
  "tercihen" demiştim, eksiklik sayılmaz.

### H-4 — KAPANDI (uygulama katmanı), kapsam sınırı var

`IInternRepository.GetByEmailAsync` + Create/Update handler kontrolleri;
Update'te "kendi kaydı hariç" var (Bölüm 1'deki regresyon riski önlenmiş).
`db/21_intern_email_unique.sql` doğru yazılmış (`Interns.Email` NOT NULL olduğu
için düz UNIQUE constraint yeterli). Uygulama katmanı script'siz de çalışıyor.
Kapsam sınırı için Bölüm 4.

---

## Bölüm 3 — Atlatma denemeleri (K1)

Guard'a **31 saldırı + 20 meşru** sorgu uygulandı. K1'in özündeki
parser-differential sınıfının tamamı engellendi; **`;` kullanmayan ifade
zincirleme sınıfında 4 vektör geçti.**

### Engellenen — parser differential (K1'in özü), 17/17

| Vektör | Girdi | Sonuç |
|---|---|---|
| Satır yorumu string içinde | `SELECT '--' AS x, 1; DROP TABLE Employees` | engellendi |
| Blok yorum string içinde | `SELECT '/*' AS x; DROP TABLE Employees; SELECT '*/' AS y` | engellendi |
| Tırnak kaçışı `''` | `SELECT 'it''s --' AS x, 1; DROP TABLE Employees` | engellendi |
| `N'...'` unicode öneki | `SELECT N'--' AS x, 1; DROP TABLE Employees` | engellendi |
| `N'...'` + blok yorum | `SELECT N'/*' AS x; TRUNCATE TABLE Employees; SELECT N'*/' AS y` | engellendi |
| Köşeli tanımlayıcı | `SELECT 1 AS [--]; DROP TABLE Employees` | engellendi |
| Kaçışlı köşeli `]]` | `SELECT 1 AS [a]]--]; DROP TABLE Employees` | engellendi |
| Çift tırnaklı tanımlayıcı | `SELECT 1 AS "--"; DROP TABLE Employees` | engellendi |
| İç içe blok yorum | `SELECT /* /* */ 1; DROP TABLE Employees` | engellendi (kapanmamış yorum) |
| `\r` ile biten satır yorumu | `SELECT 1 --\r; DROP TABLE Employees` | engellendi |
| Yorumla parçalanmış DROP | `SELECT '/*' AS a; DR/**/OP TABLE Employees; ...` | engellendi |
| Kapanmamış blok yorum | `SELECT 1 /*; DROP TABLE Employees` | engellendi |
| Kapanmamış tırnak | `SELECT 'abc; DROP TABLE Employees` | engellendi |
| Boşluk/sekme ile başlayan | `\t\n  SELECT '--' AS x, 1; DROP ...` | engellendi |
| BOM ile başlayan | `<BOM>SELECT 1; DROP TABLE Employees` | engellendi |
| `INTO` filtresi | `SELECT * INTO #tmp FROM Employees` | engellendi (`'INTO'`) |
| Yalnızca yorum | `-- hicbir sey` | engellendi |

`EXECUTE`, `EXEC xp_cmdshell`, `sp_helpdb`, `WAITFOR DELAY`, `; DBCC`, `; KILL`,
`; CHECKPOINT` de engellendi (hepsi `;` taşıdığı için).

### GEÇEN — `;` kullanmayan ifade zincirleme, 4 vektör  ⚠ KRİTİK BULGU

| Vektör | Girdi | Guard |
|---|---|---|
| Premis kanıtı | `SELECT 1 SELECT 2` | **KABUL** |
| DBCC | `SELECT 1 DBCC FREEPROCCACHE` | **KABUL** |
| DBCC (alias sonrası) | `SELECT 1 AS a DBCC CHECKDB` | **KABUL** |
| KILL | `SELECT 1 KILL 55` | **KABUL** |

**Kök neden.** Guard'ın ifade zincirlemesine karşı TEK savunması `;` aramasıdır
(`SqlReadOnlyGuard.cs:77`, *"Birden fazla ifade çalıştırılamaz."*). Oysa T-SQL
ifadeler arasında noktalı virgülü **zorunlu tutmaz**; iki ifade yalnızca boşlukla
ayrılabilir. Dolayısıyla guard'ın bu sözleşmesi tutmuyor. İkinci savunma,
`ForbiddenKeywords` listesidir — ama liste DDL/DML odaklı; **`DBCC`, `KILL`,
`CHECKPOINT`, `USE` gibi ifade başlatıcıları listede yok.** (`DROP`'lu varyant
`SELECT 1 DROP TABLE Employees` bu yüzden engelleniyor — kelime listede.)

**Ölçülülük — abartılmamalı.** Dürüst olmak gerekirse:
- Kesin olan: **guard bu metinleri kabul ediyor** ve `safeSql` olarak aynen
  döndürüyor. Bunu test ettim.
- Kesin OLMAYAN: "SQL Server bunları iki ayrı ifade olarak çalıştırır" T-SQL'in
  bilinen bir özelliğidir, ama **canlı bir sunucuda çalıştırarak doğrulamadım**
  (user-secrets'a erişim izni verilmedi, ısrar etmedim).
- Hafifletici: ikinci katman gerçekten var — `DbConnectionFactory.CreateReadOnlyConnection`
  ayrı bir `ReadOnlyConnection` dizesi istiyor ve yoksa hata fırlatıyor.
  Doğru yapılandırılmışsa (`db_datareader`) DBCC/KILL zaten yetki hatası alır;
  bunlar `ALTER SERVER STATE` / `ALTER ANY CONNECTION` ister.

Yani bu **doğrulanmış bir ayrıcalık yükseltmesi değil**, guard'ın kendi
sözleşmesini tutmadığının kanıtı ve derinlemesine savunmada gerçek bir gedik.
`ReadOnlyConnection` yanlışlıkla ayrıcalıklı bir kullanıcıya işaret ederse
(öğrenme projesinde çok olası) canlı hâle gelir.

**Kanıt kodda:** `GapClosureTests.cs` → `AtlatmaKanitlari` sınıfı, 4 test
**bilerek kırmızı**. Düzeltilince kendiliğinden yeşile döner.

**Öneri:** `ForbiddenKeywords`'e `DBCC`, `KILL`, `CHECKPOINT`, `USE`, `GO`
eklemek en ucuz kapatma. Daha sağlamı: denetlenen metinde ilk ifadeden sonra
ifade başlatıcı bir anahtar kelime gelmesini yasaklamak.

---

## Bölüm 4 — Regresyonlar / yeni sorunlar

**Kodlayıcının kendi yakaladığı regresyon (doğru ve önemli).**
`UpdateEmployeeCommandHandler.cs:103` → `employee.NationalId = nationalId ?? employee.NationalId`.
Y1 kırpması yüzünden Admin'in düzenleme formuna T.C. artık BOŞ geliyor; boş
"temizle" sayılsaydı Admin'in her kaydedişi kayıtlı T.C.'yi sessizce silerdi.
Bu, Y1 düzeltmesinin doğurduğu gerçek bir regresyondu ve kapatılmış.
Bedeli yorumda dürüstçe yazılmış: **bir kez girilmiş T.C. bu uçtan artık
temizlenemez.** Kabul edilebilir ama bilinçli bir davranış değişikliğidir.

**YENİ SORUN — Y1'in dördüncü yolu: asistan.**
Kırpma üç REST yolunda kapandı, ama `HrDatabaseSchema.cs:43` asistan modeline
`NationalId` kolonunu şema olarak tanıtıyor ve asistan
(`AskAssistantQueryHandler.cs:75`) **HR + Admin**'e açık. Kırpma kuralı ise
`EmployeeFieldVisibility`'de "yalnızca HR, Admin dahil kimse göremez" diyor.
Sonuç: bir **Admin**, `/api/employees` uçlarından göremediği T.C. kimlik
numaralarını asistana `SELECT NationalId FROM Employees` dedirterek okuyabilir.
Guard bunu engellemez (meşru bir SELECT'tir) ve engellememelidir — kural
yanlış yerde. `HrDatabaseSchema` yalnızca *"gerekmedikçe sorgulama"* diye bir
öğüt içeriyor (satır 47); öğüt yetki kontrolü değildir.
*Bu Y1'i KAPANMADI yapmaz — istenen üç yol kapandı — ama aynı kuralın kalan
deliğidir ve raporlanmalıdır.*

**H-4 kapsam sınırı.** Benzersizlik yalnızca `Interns` tablosu içinde. Bir
stajyer, var olan bir **çalışanın** e-postasıyla açılabiliyor. Bölüm 1'de
"yarı kapanma" diye işaretlemiştim; ölçülü değerlendirme: hesap açma akışı
bunu yakalıyor — `ApproveAccountRequestCommandHandler` `Users.Email` ön
kontrolü yapıp temiz bir 400 döndürüyor, 500 üretmiyor. Yani zarar "aynı
e-posta iki kişide görünebilir" ile sınırlı; çökme yok.

**H-3 küçük tutarsızlık.** Validator T.C.'yi **kırpmadan** doğruluyor
(`Length(11)`), handler ise `Trim()` ediyor. `" 12345678901 "` girişi validator'a
13 karakter görünüp reddediliyor. E-posta yolunda da aynı desen var
(`EmailAddress()` kırpmadan çalışıyor), yani yeni bir tutarsızlık değil —
düşük öncelikli.

**H-3 / H-4 TOCTOU.** İki kontrol de "önce SELECT sonra INSERT". DB kısıtları
kurulana kadar eşzamanlı iki istek mükerrer kayıt üretebilir. Script'lerin
yorumlarında bu zaten dürüstçe yazılmış.

**Test paketinde regresyon yok — assertion zayıflatması YOK.**
"Testler yeşil" tek başına kanıt değildir: testleri düzeltmeyi yapan yazdı, bir
assertion gevşetilerek de yeşile dönülebilirdi. Bu yüzden mevcut 10 test
dosyasının diff'ini ayrıca taradım:

```
$ git diff -- tests/ | grep "^-" | grep -iE "Assert\.|Throws|\.Should\("
(hiç sonuç yok)

$ git diff -- tests/ | grep "^-" | grep -cE "\[Fact\]|\[Theory\]|\[InlineData"
0

$ git diff -- tests/ | grep "^+" | grep -icE "Assert\.|Throws"
5
```

Yani: **silinen tek bir assertion yok**, **silinen tek bir test yok**
(`[Fact]`/`[Theory]`/`[InlineData]` sayısı hiç azalmamış), 5 assertion eklenmiş.
Mevcut dosyalardaki değişiklikler sahte repo'lara yeni arayüz üyesi eklemekten
ibaret. Taban 110 testin hepsi hâlâ yeşil: 110 → 163 (+53), başarısız 0.

---

## Bölüm 5 — Kalan iş

1. **[KRİTİK] `;` olmadan ifade zincirleme** — Bölüm 3. `ForbiddenKeywords`'e
   `DBCC` / `KILL` / `CHECKPOINT` / `USE` eklenmeli ya da ifade başlatıcı denetimi
   getirilmeli. Kanıt: `GapClosureTests.AtlatmaKanitlari` (4 kırmızı test).
2. **`ReadOnlyConnection` gerçekten `db_datareader` mı?** Kod tarafı doğru
   (ayrı dize, yoksa hata). Yapılandırmanın kendisini doğrulayamadım
   (user-secrets). Madde 1'in gerçek etkisi buna bağlı — teyit edilmeli.
3. **H-1 mevcut veri.** `Pending` durumda asılı, mentoru olmayan stajyer
   talepleri İK listesinde görünmüyor (Admin görüyor). Ya backfill
   (`Pending` → `PendingHr`) ya da `GetPendingApprovalsQueryHandler`'ın
   `Pending` dalına "mentorsuz stajyer → İK" kuralı.
4. **Y1'in asistan yolu.** Admin'in T.C.'yi asistan üzerinden okuyabilmesi
   (Bölüm 4). Ya şemadan `NationalId` çıkarılmalı, ya asistan yalnızca HR'a
   açılmalı, ya da kolon sorgu düzeyinde engellenmeli.
5. **DB script'leri çalıştırılmadı.** `db/20_employee_nationalid_unique.sql`
   ve `db/21_intern_email_unique.sql` yazıldı ama uygulanmadı. Uygulama katmanı
   onlarsız da doğru davranıyor; asıl garanti (ve TOCTOU kapanışı) script'lerle gelir.
6. **`GetAll` ucundaki `Intern` rolü** — kullanıcı kararı bekliyor, KAPANMADI
   sayılmadı. Karar "kapatılsın" olursa `InternsController.cs:38` `HR,Admin`
   olmalı; WebUI'daki üç tüketicinin üçü de zaten HR/Admin'e kilitli olduğu için
   regresyon beklenmez.
7. **WebUI'da kalan `DateTime.Today`** — `WebUI/Controllers/LeaveRequestsController.cs`
   satır 484 ve 553 karar mantığında yerel saat kullanıyor (213/227 yalnızca
   rapor başlığı/dosya adı, zararsız). H-5'in kapsamı Application'dı, bu yüzden
   KAPANMADI saymadım; ama aynı sınıf hata.
8. **Loglama çalışma anında denenmedi** — derleme ve DI kaydı doğru, API
   çalıştırılıp gerçek bir 500 üretilerek teyit edilmedi.

---

## Ek — Komut çıktıları

```
$ dotnet build -t:Compile
    0 Uyarı
    0 Hata
```
*(Düz `dotnet build` MSB3021/MSB3027 verir: API PID 28268 ve WebUI PID 19612
çalışır durumda, bin/ kopyalaması kilitli. Derleme hatası değil.)*

```
$ dotnet test          # doğrulama testleri EKLENMEDEN önce
Başarılı!  - Başarısız: 0, Başarılı: 163, Atlanan: 0, Toplam: 163

$ dotnet test          # Verification/GapClosureTests.cs eklendikten sonra
Başarısız! - Başarısız: 4, Başarılı: 203, Atlanan: 0, Toplam: 207
```

Başarısız 4 testin tamamı `Verification/GapClosureTests.cs` → `AtlatmaKanitlari`
sınıfındadır ve **bilerek kırmızıdır** (Bölüm 3'teki açık kapanınca yeşile döner).
Eklediğim 44 testin 40'ı yeşil; kodlayıcının 163 testinin hiçbiri kırılmadı.

Doğrulama sırasında benim yazdığım/değiştirdiğim tek dosyalar:
- `docs/dogrulama-raporu.md` (bu rapor)
- `tests/HRManagement.Application.Tests/Verification/GapClosureTests.cs`

`src/` altında **hiçbir** dosyaya dokunulmadı; `git add` / `commit` / `stash` /
`checkout` / `reset` **çalıştırılmadı**.
