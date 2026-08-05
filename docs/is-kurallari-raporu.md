# İş Kuralları Denetim Raporu — HRManagement

**Tarih:** 2026-08-05
**Kapsam:** İş kurallarının DOĞRULUĞU, tutarlılığı ve eksikleri. Yetki/güvenlik konuları
ayrı bir denetimin konusu; buraya yalnızca "kural doğru mu hesaplanıyor" sorusu girdi.
**Test durumu:** `dotnet test` → 110/110 yeşil (doğrulandı, süre 118 ms).

---

## Özet

| Bölüm | Sayı |
|---|---|
| 1 — Hatalar | 5 |
| 2 — Riskler | 13 |
| 3 — Karar gerektirenler | 8 |
| 4 — Test boşlukları | 14 |
| 5 — İyi çözülmüş kurallar | 15 |

**En ciddi bulgu (H-1):** Mentoru olmayan bir stajyerin izin talebi onay akışında
kilitleniyor. Talep `Pending` doğuyor, ama `LeaveApprovalGuard` stajyerin zincirini
mentordan başlattığı için `MentorId = NULL` olduğunda **Admin dışında hiç kimse** —
İK dahil — o talebi ne görebiliyor ne işleyebiliyor. Çalışan tarafında aynı durum
için (`ManagerId = NULL`) bilinçli bir çözüm var; stajyer tarafına uygulanmamış.

**Diğer iki kritik madde:**
- **R-1:** Bakiye yarış durumuna karşı konan onay-anı yeniden denetimi, `PendingHr`
  olarak DOĞAN taleplerde (tepe yönetici + hastalık izni) hiç çalışmıyor.
- **H-3/H-4:** T.C. Kimlik No ve stajyer e-postası için hiçbir katmanda benzersizlik
  kontrolü yok (çalışan e-postasında hem DB kısıtı hem uygulama kontrolü varken).

**Notlar:**
- `LeaveEntitlement`'ın matematiği (kıdem kademeleri, hak ediş toplamı, avans sınırı,
  iş günü sayımı) satır satır izlendi ve **hatasız** bulundu. En sık yapılan iki hata
  (5. yılı üst kademeye koymak, 15. yılı alt kademede bırakmak) burada doğru yapılmış
  ve testlenmiş. Bu bölümde hesap hatası yok; bulgular hesabın *etrafındaki* kurallarda.
- Bölüm 1'deki 5 maddenin 3'ü "yanlış hesap" değil, "hiç uygulanmayan kural"; her
  maddede hangisi olduğu ayrıca yazıldı.

---

## Bölüm 1 — Hatalar

### H-1 · Mentoru olmayan stajyerin izin talebi akışta kilitlenir

**Dosyalar:**
`Application/Features/LeaveRequests/Commands/CreateLeaveRequest/CreateLeaveRequestCommandHandler.cs:82-87`
`Application/Features/LeaveRequests/Shared/LeaveApprovalGuard.cs:99-108`
`Application/Features/LeaveRequests/Queries/GetPendingApprovals/GetPendingApprovalsQueryHandler.cs:59-63`

**Girdi:** `MentorId = NULL` olan bir stajyer, ücretsiz izin talebi açıyor.
`MentorId` hiçbir katmanda zorunlu değil — DB'de `MentorId int NULL`
(`db/05_full_setup.sql:145`), `CreateInternCommand.MentorId` tipi `int?`, ve
`CreateInternCommandValidator`'da MentorId için **hiçbir kural yok**.

**Kodun ürettiği davranış — adım adım:**

1. `CreateLeaveRequestCommandHandler.cs:82`
   ```csharp
   var skipManagerStage = request.Type == LeaveType.Sick
       || (employee is not null && employee.ManagerId is null);
   ```
   Talep sahibi stajyer olduğu için `employee` null → ikinci koşul **false**.
   Tür `Unpaid` → ilk koşul da false. Sonuç: `initialStatus = LeaveStatus.Pending`.

2. `LeaveApprovalGuard.cs:81-113` (`EnsureManagerStageAsync`) — Admin değilse:
   ```csharp
   if (intern?.MentorId is int mentorId)
       authorized = actorEmployee.Id == mentorId || await IsInManagerChainAsync(...);
   ```
   `MentorId` null → `if` hiç girilmez → `authorized` false kalır → satır 111'de
   *"Bu talebi yönetici aşamasında işleme yetkiniz yok"* fırlatılır. İK rolü bu
   dalda hiç sorgulanmıyor (İK yalnızca `PendingHr` aşamasında yetkili).

3. `GetPendingApprovalsQueryHandler.cs:59-63` — okuma yolu da simetrik olarak
   `c.MentorId is int mid` koşuluna takılıyor. Talep **İK'nın "Onay Bekleyenler"
   listesinde bile görünmüyor.**

**Sonuç:** Talep `Pending`'de sonsuza kadar asılı kalır. Yalnızca Admin (satır 83-84,
"kilit çözücü") işleyebilir. Stajyerin kendisi de silebilir (`DeleteLeaveRequest`),
ama onaylanma yolu yok.

**Olması gereken:** Çalışan tarafındaki kararın (2026-08-03: "onaylayacak üstü
olmayan kişinin talebi doğrudan İK'ya düşer") stajyer karşılığı uygulanmalı.
`CreateLeaveRequestCommandHandler.cs:81`'deki yorum *"Stajyeri etkilemez: onun onay
zinciri mentordan başlar, tepe olamaz"* diyor — ama mentoru **hiç olmayabileceği**
gözden kaçmış.

**Düzeltme önerisi:**
```csharp
var skipManagerStage = request.Type == LeaveType.Sick
    || (employee is not null && employee.ManagerId is null)
    || (intern   is not null && intern.MentorId  is null);   // ← eklenecek
```
Alternatif (daha güçlü): `CreateInternCommandValidator`'da `MentorId`'yi zorunlu
kılmak. İkisi birlikte yapılırsa mevcut mentorsuz kayıtlar da kurtulur.

---

### H-2 · Sınıf yorumundaki kıdem kademesi kodla ve kanunla çelişiyor

**Dosya:** `Application/Services/LeaveEntitlement.cs:18`

```
/// Kademeler (İş Kanunu md. 53): 1–5. yıl 14 gün, 6–15. yıl 20 gün, 15+ yıl 26 gün.
```

15. yıl **iki aralıkta birden** görünüyor. Kod (satır 39-45):

```csharp
<= 5  => 14,   // 1–5. yıl
< 15  => 20,   // 6–14. yıl
_     => 26    // 15. yıl ve sonrası
```

**Girdi → çıktı:** `GrantForYear(15)` → **26**.
**Doğru çıktı:** 26. İş Kanunu md. 53: *"Onbeş yıl (dahil) ve daha fazla olanlara
yirmialtı günden az olamaz."* → **kod doğru, satır 43'teki inline yorum doğru, testler
doğru** (`LeaveEntitlementTests.cs:49` bunu açıkça test ediyor).

**Yanlış olan tek şey sınıf başlığındaki yorum.** Hesap hatası üretmiyor; ama bu
sınıfın tek kaynak (single source of truth) olduğu ve gelecekte kademe düzenlenirken
ilk okunacak yerin başlık yorumu olduğu düşünülürse, "yorumu düzelteyim" refleksiyle
`< 15` → `<= 15` yapılması gerçek ve sessiz bir hata üretir (15. yıl 26 yerine 20 gün).

**Düzeltme:** Satır 18 → `1–5. yıl 14 gün, 6–14. yıl 20 gün, 15. yıl ve sonrası 26 gün.`

---

### H-3 · T.C. Kimlik No için hiçbir katmanda benzersizlik veya format kuralı yok

*(Bu bir "yanlış hesap" değil, hiç uygulanmayan bir kural.)*

**Kanıtlar:**
- **DB:** `db/05_full_setup.sql:106` → `NationalId nvarchar(11) NULL`. Aynı dosyanın
  kısıt bloğunda (satır 122-127) `UQ_Employees_Email` var, `NationalId` için
  UNIQUE **yok**, CHECK **yok**.
- **Validator:** `CreateEmployeeCommandValidator.cs` ve `UpdateEmployeeCommandValidator.cs`
  içinde `NationalId` kelimesi hiç geçmiyor (proje geneli grep ile doğrulandı; alan
  yalnızca command, entity, DTO, mapping ve görünürlük kırpmasında geçiyor).
- **Handler:** `CreateEmployeeCommandHandler.cs:76` ve `UpdateEmployeeCommandHandler.cs:84`
  değeri hiçbir kontrolden geçirmeden atıyor.

**Girdi → yanlış çıktı:**
| Girdi | Kodun ürettiği | Olması gereken |
|---|---|---|
| İki çalışan, aynı `NationalId = "12345678901"` | İkisi de kaydedilir | 2.'si reddedilir |
| `NationalId = "abc"` | Kaydedilir | Reddedilir (11 hane, rakam) |
| `NationalId = "1"` | Kaydedilir | Reddedilir |

Aynı kişinin iki kez kaydedilmesi izin bakiyesini de ikiye böler (`GetTotalUsedAnnualDaysAsync`
`EmployeeId` bazlı çalışır), yani sonuçları izin modülüne kadar uzanır.

**Düzeltme önerisi:**
1. Validator'a: `Length(11)` + `Matches("^[0-9]{11}$")` (`.When(x => !string.IsNullOrWhiteSpace(x.NationalId))`).
   İsteğe bağlı: T.C. checksum algoritması.
2. Handler'a e-posta ile aynı desen: `GetByNationalIdAsync` ile ön kontrol + anlaşılır 400.
3. DB'ye filtered unique index (NULL'lar serbest kalsın):
   `CREATE UNIQUE INDEX UX_Employees_NationalId ON dbo.Employees(NationalId) WHERE NationalId IS NOT NULL;`

---

### H-4 · Stajyer e-postasında benzersizlik yok — çalışanla asimetrik

*(Yine "uygulanmayan kural" tipi.)*

**Çalışan tarafı (doğru kurulmuş):**
- DB kısıtı: `db/05_full_setup.sql:122` → `CONSTRAINT UQ_Employees_Email UNIQUE (Email)`
- Uygulama ön kontrolü: `CreateEmployeeCommandHandler.cs:55-56` — üstelik gerekçesi de
  yazılı: *"DB'de UNIQUE kısıt var, ama ona takılmak 500 üretir. Kuralı burada önden
  uygulayıp anlaşılır bir 400 mesajı veriyoruz."*
- Güncellemede kendi kaydı hariç kontrol: `UpdateEmployeeCommandHandler.cs:54-56`

**Stajyer tarafı:**
- DB: `db/05_full_setup.sql:136` → `Email nvarchar(100) NOT NULL`, UNIQUE kısıtı **yok**
  (satır 150-153'teki kısıt listesinde yalnızca PK ve 3 FK var).
- `CreateInternCommandHandler.cs:35` → `Email = request.Email.Trim()`, kontrol yok.
- `UpdateInternCommandHandler.cs:35` → aynı, kontrol yok.
- `CreateInternCommandValidator.cs:22-24` → yalnızca `NotEmpty` + `EmailAddress` (format).

**Girdi → yanlış çıktı:** `ali@x.com` ile iki stajyer kaydı açılır; ikisi de başarılı
döner. Olması gereken: 2. kayıt *"Bu e-posta ile kayıtlı bir stajyer zaten var."*
ile reddedilmeli.

**Ek not:** Çalışan e-postası ile stajyer e-postası arasında da çapraz kontrol yok —
aynı e-posta hem bir çalışanda hem bir stajyerde bulunabilir. Bunun istenip
istenmediği bir karar (bkz. K-6).

**Düzeltme:** `IInternRepository`'ye `GetByEmailAsync` ekleyip çalışandaki deseni
birebir tekrarlamak + `UQ_Interns_Email` kısıtı.

---

### H-5 · `DateTime.Today` ile `DateTime.UtcNow.Date` karışık kullanılıyor — sınır saatlerde iki katman çelişiyor

**Dosyalar:**
- Kural katmanı **UTC** kullanıyor: `CreateLeaveRequestCommandHandler.cs:46,112`,
  `ApproveLeaveRequestCommandHandler.cs:106`, `DeleteLeaveRequestCommandHandler.cs:60`,
  `EmployeeDetailAssembler.cs:150`, `GetAllEmployeesQueryHandler.cs:47`,
  `GetHrDashboardQueryHandler.cs:58`
- **YEREL** saat kullanan yerler:
  - `Application/Features/Organization/Queries/GetOrganization/GetOrganizationQueryHandler.cs:74`
    → `IsActive = i.EndDate.Date >= DateTime.Today` ← **Application katmanında tek yerel kullanım**
  - `WebUI/Views/LeaveRequests/Index.cshtml:234` → `izinBaslamadi = request.StartDate.Date > DateTime.Today`
  - `WebUI/Controllers/LeaveRequestsController.cs:484,553` (filtre aralıkları, ekip takvimi)
  - `WebUI/Views/Interns/Index.cshtml:11`, `Mentorship/Index.cshtml:7`, `Mentorship/Detail.cshtml:23`,
    `Home/InternHome.cshtml:8`, `InternProfile/Index.cshtml:11`, `MyTasks/Index.cshtml:10,62`

**Somut senaryo (sunucu Türkiye saatinde, UTC+3):**
Yerel saat **2026-08-06 01:00** → `DateTime.Today` = **2026-08-06**,
`DateTime.UtcNow.Date` = **2026-08-05**. Her gün 00:00–03:00 arası 3 saatlik pencerede
ikisi farklı gün gösterir.

| Durum | Yerel katmanın dediği | UTC katmanın dediği | Sonuç |
|---|---|---|---|
| İzni 2026-08-06'da başlayan talep, saat 01:00 | `06 > 06` false → *"İzin başlamış, İptal düğmesini gizle"* (Index.cshtml:234) | `06 <= 05` false → *"iptal edilebilir"* (DeleteLeaveRequest:60) | Kullanıcı iptal edemiyor sanır; oysa API kabul ederdi |
| Aynı senaryo, izni 2026-08-05'te başlayan talep | `05 > 06` false → düğme gizli | `05 <= 05` **true** → API reddeder | Bu tarafta tutarlı |
| Stajın bitişi 2026-08-05, saat 01:00 | `05 >= 06` false → org şemasında **pasif** | `05 < 05` false → izin talebi **kabul edilir** | İki ekran aynı stajyer için farklı şey söyler |

**Olması gereken:** Tek bir "bugün" kaynağı. `db/18_sp_hr_dashboard.sql:33` bu ilkeyi
zaten açıkça yazmış (*"@Today PARAMETRE, GETDATE() DEĞİL. Uygulama 'bugün'ü UTC olarak
geçirir"*) — DB tarafı disiplinli, uygulama tarafı bir yerde kaçırmış.

**Düzeltme önerisi:**
1. `GetOrganizationQueryHandler.cs:74` → `DateTime.UtcNow.Date` (tek satırlık, hemen).
2. WebUI'daki `DateTime.Today` kullanımları UX amaçlı; ama karar veren yerler
   (Index.cshtml:234'teki düğme gizleme) API ile aynı kaynağı kullanmalı.
   Temiz çözüm: bu kararı API'nin döndürdüğü bir alana (`CanCancel`) taşımak —
   "otorite her zaman API + Application'dır" kuralıyla da (CLAUDE.md) uyumlu olur.
3. Uzun vadede: `IClock`/`TimeProvider` soyutlaması. Yan fayda: `LeaveEntitlement`
   dışındaki tarih kuralları da DB'siz test edilebilir hale gelir (bkz. Bölüm 4).

---

## Bölüm 2 — Riskler

### R-1 · Onay anındaki bakiye yeniden denetimi, `PendingHr` doğan taleplerde HİÇ çalışmıyor ★

**Dosya:** `ApproveLeaveRequestCommandHandler.cs:49-73`

```csharp
switch (leaveRequest.Status)
{
    case LeaveStatus.Pending:
        if (leaveRequest.Type == LeaveType.Annual && leaveRequest.EmployeeId is int employeeId)
            await EnsureBalanceStillSufficientAsync(employeeId);   // ← yalnız burada
        ...
    case LeaveStatus.PendingHr:
        // bakiye denetimi YOK
        leaveRequest.Status = LeaveStatus.Approved;
        break;
}
```

Yeniden denetim yalnızca `Pending` dalında. Ama `CreateLeaveRequestCommandHandler.cs:82-87`
uyarınca iki hâl `PendingHr` olarak **doğuyor**:
- Hastalık izni → `Type != Annual` olduğu için zaten kontrol dışı, sorun yok.
- **Zincirin tepesindeki çalışan (`ManagerId is null`)** → `Annual` talebi doğrudan
  `PendingHr` doğar ve `Pending` dalına **hiç uğramaz**.

**Sonuç:** GM/tepe yönetici için yıllık izin talebi, oluşturma anındaki tek kontrolle
onaylanır. R-2'deki yarış durumu bu kullanıcılar için hiçbir ağ tarafından yakalanmaz.

**Düzeltme:** Bakiye denetimini `switch`'in dışına, guard'dan hemen sonraya taşımak:
```csharp
if (leaveRequest.Type == LeaveType.Annual && leaveRequest.EmployeeId is int employeeId)
    await EnsureBalanceStillSufficientAsync(employeeId);
```
`used > accrued + nextGrant` koşulu her iki aşamada da doğru çalışır (talep zaten
"kullanılan"ın içindedir), bu yüzden taşıma güvenli.

---

### R-2 · İzin bakiyesi kontrolü — kontrol-sonra-yaz (TOCTOU), transaction yok

**Dosya:** `CreateLeaveRequestCommandHandler.cs:110-125` (kontrol) → `:102` (yazma)

`EnsureAnnualBalanceAsync` okur, `AddAsync` yazar; arada transaction, kilit veya DB
kısıtı yok. `LeaveRequestRepository.AddAsync` (satır 39-47) düz bir INSERT.

**Senaryo:** 3 yıllık çalışan (accrued 42, nextGrant 14 → sınır 56), `used = 50`.
İki sekmede aynı anda 5'er günlük iki talep gönderiyor:

| | İstek A | İstek B |
|---|---|---|
| `GetTotalUsedAnnualDaysAsync` | 50 | 50 |
| Kontrol | 50 + 5 = 55 ≤ 56 ✔ | 50 + 5 = 55 ≤ 56 ✔ |
| INSERT | ✔ | ✔ |
| **Gerçek toplam** | **60 > 56** | |

**Kısmi bağışıklık:** Onay anındaki yeniden denetim (`EnsureBalanceStillSufficientAsync`)
`used = 60 > 56` görüp **her iki talebi de** reddeder. Yani hatalı veri onaylanmaz —
ama iki talep de sonradan patlar, ve R-1'deki durumda bu ağ hiç yoktur.

**Düzeltme seçenekleri:**
- **En küçük:** Kontrol + INSERT'ü tek transaction'a alıp `SELECT ... WITH (UPDLOCK, HOLDLOCK)`
  ile çalışan satırını kilitlemek. (`UserRepository.CreateForPersonAsync`'te zaten
  bu desenin altyapısı var.)
- **Alternatif:** INSERT'ü koşullu tek ifadeye çevirmek
  (`INSERT ... SELECT ... WHERE (SELECT SUM ...) + @requested <= @limit`) ve `@@ROWCOUNT`'a bakmak.

---

### R-3 · Tarih çakışması kontrolü — TOCTOU, hiçbir ağ yok

**Dosya:** `CreateLeaveRequestCommandHandler.cs:50-52` → `:102`;
SQL: `LeaveRequestRepository.cs:107-140`

`HasOverlapAsync` mantığı **doğru** (yarı açık aralık kesişimi, `Cancelled`/`Rejected`
hariç). Sorun mantıkta değil, zamanlamada: iki eşzamanlı istek aynı tarihler için
gönderilirse ikisi de `false` alır ve ikisi de kaydedilir.

**R-2'den daha kötü olan yanı:** Bakiye kuralının onay anında ikinci bir denetimi
var; **çakışmanın yok**. Approve handler'ı `HasOverlapAsync`'i hiç çağırmıyor. İki
çakışan talep ikisi de onaylanabilir ve `OnLeaveNowCount` gibi raporlar aynı kişiyi
iki kez sayar.

**Düzeltme:** R-2 ile aynı transaction/kilit içinde çözülür. DB seviyesinde tam
çözüm için SQL Server'da aralık kısıtı yok; pratik yol tek transaction + `HOLDLOCK`.

---

### R-4 · "Bir hesap = bir kişi" kuralı yalnızca uygulamada; DB'de garanti yok

**Uygulama kontrolü:** `CreateEmployeeCommandHandler.cs:115-125`,
`UpdateEmployeeCommandHandler.cs:195-205` (`EnsureUserLinkableAsync`)

**DB durumu:** `db/05_full_setup.sql:518` → `CREATE INDEX IX_Employees_UserId` —
**UNIQUE değil**, düz index. `Interns.UserId` için index bile yok. Yani iki
eşzamanlı `CreateEmployee` (ya da `CreateEmployee` + `CreateUserForPerson`) aynı
`UserId`'yi iki kişiye bağlayabilir ve DB itiraz etmez.

**Sonrası:** `EmployeeRepository.GetByUserIdAsync` (satır 153-162) bu ihtimali zaten
biliyor ve `QueryFirstOrDefaultAsync` kullanıyor — yorumu açık: *"iş kuralı bir
hesabı tek çalışana bağlasa da, elle girilmiş mükerrer bir kayıt tüm giriş akışını
500'e çevirmemeli"*. Savunma doğru; ama sonuç, mükerrer kayıtta **hangi çalışanın
"ben" olduğunun satır sırasına kalması**. İzin talebi, bakiye, onay zinciri — hepsi
yanlış kişiye bağlanabilir.

**Düzeltme:**
```sql
CREATE UNIQUE INDEX UX_Employees_UserId ON dbo.Employees(UserId) WHERE UserId IS NOT NULL;
CREATE UNIQUE INDEX UX_Interns_UserId   ON dbo.Interns(UserId)   WHERE UserId IS NOT NULL;
```
(Filtered index NULL'ları serbest bırakır — hesabı olmayan çalışan/stajyer normaldir.)

---

### R-5 · Aynı hesap talebini iki Admin aynı anda onaylarsa öksüz kullanıcı kalır

**Dosya:** `ApproveAccountRequestCommandHandler.cs:38-67`

Durum kontrolü (`Status != Pending` → reddet, satır 38) ve "bu arada hesap açılmış mı"
kontrolü (satır 42, `EnsureSubjectStillNeedsAccountAsync`) **transaction'ın DIŞINDA**.
Transaction `UserRepository.CreateForPersonAsync:106`'da başlıyor.

**Senaryo:** İki Admin aynı Id'yi aynı anda onaylıyor.
- İkisi de `Status == Pending` görür ✔
- İkisi de `employee.UserId is null` görür ✔
- İkisi de yeni bir `Users` satırı yaratır (Username/Email UNIQUE olduğu için
  **farklı** kullanıcı adı girmişlerse ikisi de başarılı olur)
- İkisi de `UPDATE Employees SET UserId = @newUserId` çalıştırır → **son yazan kazanır**
- Sonuç: bir çalışana bağlı 1 hesap + **hiçbir yere bağlı olmayan, aktif, giriş
  yapabilen 1 hesap** kalır.

`UX_AccountRequests_PendingEmployee` filtered unique index (`db/06_account_requests.sql:52`)
*farklı* taleplerin çoğalmasını engelliyor — bu iyi bir savunma — ama **aynı** talebin
iki kez işlenmesini engellemiyor.

**Düzeltme:** Talep kapatmayı transaction içinde koşullu yapıp satır sayısını kontrol
etmek (`UserRepository.cs:94-98`'deki `closeRequest` sorgusu):
```sql
UPDATE AccountRequests SET Status = @Approved, ... WHERE Id = @RequestId AND Status = @Pending
```
→ `@@ROWCOUNT = 0` ise rollback + *"Bu talep başka bir yönetici tarafından işlendi."*

---

### R-6 · Onay/red yarışı: iyimser eşzamanlılık denetimi yok

**Dosyalar:** `ApproveLeaveRequestCommandHandler.cs:40-75`,
`RejectLeaveRequestCommandHandler.cs:30-42`, `LeaveRequestRepository.cs:49-71`

Her ikisi de klasik oku-değiştir-yaz. `UpdateAsync` koşulsuz: `WHERE Id = @Id`.
Ne satır sürümü (rowversion) ne durum koşulu var.

**Senaryo A — onay + red aynı anda (`PendingHr`):**
İki İK uzmanı, biri Onayla biri Reddet. İkisi de `Status = PendingHr` okur, ikisi de
guard'dan geçer. Son yazan kazanır. Talep `Approved` olur ama `RejectedByUserId`,
`RejectedAt`, `RejectionReason` **dolu** kalır (Approve handler'ı bu alanları
temizlemiyor) — ya da tersi: `Rejected` olur ama `HrApprovedByUserId` dolu kalır.
Denetim izi kendi kendisiyle çelişir.

**Senaryo B — "iki ayrı göz" kuralı ve yarış:**
İki farklı yönetici aynı anda `Pending` talebi onaylıyor. İkisi de `Pending` okur,
ikisi de `Pending` dalını çalıştırır, `ManagerApprovedByUserId` son yazanınki olur.
Sonra `EnsureHrStage` (`LeaveApprovalGuard.cs:123`) yalnızca **o tek kişiyi** İK
aşamasından men eder — kayıt tutulmayan diğer onaylayan, İK rolündeyse ikinci
aşamayı da kendisi işleyebilir. "İki ayrı göz" kuralı bu dar pencerede delinir.

**Düzeltme:** `UpdateAsync`'e beklenen durumu eklemek —
`WHERE Id = @Id AND Status = @ExpectedStatus` — ve `@@ROWCOUNT = 0` ise
*"Bu talebin durumu bu arada değişti; sayfayı yenileyin."* Bu tek değişiklik
Senaryo A ve B'yi birlikte kapatır.

---

### R-7 · Benzersizlik yarışlarının DB tarafından yakalanan kısmı 400 değil 500 üretir

**Yerler:** `CreateUserForPersonCommandHandler.cs:38-42`, `ApproveAccountRequestCommandHandler.cs:47-51`,
`CreateEmployeeCommandHandler.cs:55`, `UpdateEmployeeCommandHandler.cs:54`

Burada **veri güvende**: `UQ_Users_Username`, `UQ_Users_Email`, `UQ_Employees_Email`
kısıtları (`db/05_full_setup.sql:88-89,122`) yarışı yakalar. Ama uygulama ön kontrolü
geçtikten sonra INSERT patlarsa `GlobalExceptionHandler` bunu beklenmeyen hata sayar
→ **500 + genel mesaj**, oysa kullanıcının görmesi gereken 400 + *"Bu kullanıcı adı
zaten kullanılıyor."*

Düşük şiddetli (nadir pencere, veri bozulmuyor), ama `CreateEmployeeCommandHandler.cs:53-54`'teki
yorum bu 500'ü önlemeyi açıkça amaçlıyor — yarış durumunda amaç tutmuyor.

**Düzeltme:** `SqlException.Number` 2601/2627'yi yakalayıp `ValidationException`'a
çevirmek (repository sınırında).

---

### R-8 · İzin süresi için üst sınır yok — kontrolsüz döngü

**Dosyalar:** `CreateLeaveRequestCommandValidator.cs:27-29`, `LeaveEntitlement.cs:85-98`

Validator'daki tek tarih kuralı: `EndDate > StartDate`. Üst sınır yok.
`WorkingDays` günü gün ilerleyen bir döngü:
```csharp
for (var day = start; day < end; day = day.AddDays(1))
```

**Girdi:** `StartDate = 0001-01-01`, `EndDate = 9999-12-31`, `Type = Unpaid`.
**Kodun ürettiği:** ~3.650.000 iterasyon (her istek için, üstelik `Annual` ise
`EnsureAnnualBalanceAsync:118`'de **bir kez daha** hesaplandığı için iki kat),
sonra `WorkingDays ≈ 2.607.000` değeriyle kayıt oluşur.
**Olması gereken:** *"İzin süresi en fazla N gün olabilir."* ile 400.

Ücretsiz izinde hiçbir kural bunu durdurmuyor (bakiye kontrolü yalnızca `Annual` için).

**Düzeltme:**
```csharp
RuleFor(c => c.EndDate)
    .LessThanOrEqualTo(c => c.StartDate.AddDays(366))
    .WithMessage("Tek bir izin talebi 1 yıldan uzun olamaz.");
```
Ek olarak `WorkingDays`'i döngü yerine kapalı formülle hesaplamak (tam hafta sayısı ×5
+ artık günler) hem O(1) yapar hem bu sınıfı tamamen bağışık kılar.

---

### R-9 · Geçmişe dönük izin talebi hiçbir kuralla sınırlanmıyor

Ne validator ne handler `StartDate`'in bugüne göre konumuna bakıyor. Bir çalışan
2020 yılı için `Annual` talep açabilir; çakışma yoksa kabul edilir ve **kümülatif
bakiyeden düşer**. Onaylayan taraf da geçmiş tarihli talebi normal akışta onaylar.

Hastalık izni için geriye dönüklük meşru (rapor sonradan gelir) — bu yüzden blanket
bir yasak yanlış olur. Karar gerektiren tarafı K-4'te.

---

### R-10 · Stajyerin izin tarihleri staj dönemi dışına taşabilir

**Dosya:** `CreateLeaveRequestCommandHandler.cs:46`

```csharp
if (intern is not null && intern.EndDate.Date < DateTime.UtcNow.Date)
    throw new ValidationException("Staj süresi sona ermiş; izin talebi oluşturulamaz.");
```

Kontrol edilen tek şey: *bugün* stajın bitişini geçmiş mi. Talebin **kendi tarihleri**
staj dönemiyle karşılaştırılmıyor.

**Girdi:** Stajı `2026-06-01 → 2026-09-01` olan stajyer, bugün 2026-08-05,
`StartDate = 2027-03-01`, `EndDate = 2027-03-10`, `Unpaid`.
**Kodun ürettiği:** Kabul (bugün < 09-01, çakışma yok, tür Annual değil).
**Olması gereken:** *"İzin tarihleri staj dönemi dışında."* ile reddedilmeli.

**Düzeltme:** `intern is not null` dalına ek kural —
`request.StartDate.Date >= intern.StartDate.Date && request.EndDate.Date <= intern.EndDate.Date.AddDays(1)`
(bitiş yarı açık olduğu için +1).

---

### R-11 · Resmî tatiller iş gününden düşülmüyor

**Dosya:** `LeaveEntitlement.cs:81-82` — bilinen ve yorumda yazılı eksik
(*"resmi tatiller şimdilik sayılır (tatil tablosu eklendiğinde buraya girer)"*).

**Somut bedeli:** 2026 Kurban Bayramı'na denk gelen Pzt→ertesi Pzt izni,
`WorkingDays` = **5** döner. Gerçekte 1-2 iş günü izin kullanılmıştır. Çalışan her
bayram haftası için hakkından 3-4 gün fazla harcamış olur; bu kümülatif modelde
kalıcıdır ve yıl dönümünde temizlenmez.

**Düzeltme:** `Holidays` tablosu + `WorkingDays(start, end, IReadOnlySet<DateTime> holidays)`
aşırı yüklemesi. Saf fonksiyon olma özelliği (ve DB'siz test edilebilirliği) korunur;
tatil listesi parametre olarak girer.

---

### R-12 · Saklanan `WorkingDays` ile tarihler arasında bağ yok; eski satırlar takvim günüyle dolduruldu

**(a) Yapısal tuzak:** `LeaveRequestRepository.UpdateAsync:53-68` `StartDate` ve
`EndDate`'i günceller ama `WorkingDays`'i **güncellemez**. Bugün tarihleri değiştiren
bir komut yok (Approve/Reject yalnızca durum alanlarını değiştirir), yani şu an
sorun üretmiyor. Ama "izin tarihini düzenle" özelliği eklendiği gün
`GetTotalUsedAnnualDaysAsync` (bu sütunu `SUM`'lar, satır 262-286) **sessizce yanlış
bakiye** döndürmeye başlar.

**(b) Mevcut veri:** `db/10_leave_rules.sql:22`
```sql
UPDATE dbo.LeaveRequests SET WorkingDays = DATEDIFF(DAY, StartDate, EndDate) + 1 WHERE WorkingDays = 0
```
Bu **takvim günü**, iş günü değil. O tarihte `EndDate` "son izinli gün" anlamındaydı:
- Pzt→Cum (tam iş haftası): DATEDIFF 4 + 1 = 5 → **doğru**
- Pzt→Paz (hafta sonunu içeren): 6 + 1 = 7 → **gerçekte 5**, 2 gün fazla
- İki haftalık izin: 14 → **gerçekte 10**, 4 gün fazla

Betiğin kendisi bunu kabul ediyor (*"Test verisi olduğu için yaklaşık yeterli"*), yani
bilinçli. **Ama:** bu betiğin çalıştığı veritabanı canlıya taşındıysa, o satırların
sahipleri bugün olduğundan az bakiye görüyor. Kontrol edilmeli.

**Doğrulama sorgusu:**
```sql
SELECT Id, StartDate, EndDate, WorkingDays, DATEDIFF(DAY, StartDate, EndDate) AS TakvimGunu
FROM dbo.LeaveRequests
WHERE WorkingDays > DATEDIFF(DAY, StartDate, EndDate) - (DATEDIFF(WEEK, StartDate, EndDate) * 2);
```

**Not — 19 numaralı migration DOĞRU:** `db/19_leave_enddate_isbasi.sql` `EndDate`'i
+1 kaydırırken `WorkingDays`'e dokunmama gerekçesi *("eski anlam uçlar dahil; kaydırma
sonrası yeni anlam bitiş hariç → aynı gün kümesi")* matematiksel olarak doğru.
Sorun 19'da değil, 10'daki kaba backfill'de.

---

### R-13 · `UnitManagerResolver` beraberlikte belirsiz sonuç veriyor

**Dosya:** `Application/Features/Interns/Shared/UnitManagerResolver.cs:37-40,47-50`

```csharp
.Where(e => e.DepartmentId == departmentId && e.UnitId == uid)
.OrderBy(e => e.Seniority)
.FirstOrDefault();
```

Aynı birimde **iki Müdür** varsa `OrderBy` ikisini de aynı sıraya koyar; `FirstOrDefault`
`GetAllAsync()`'in (`SELECT * FROM Employees`, sıralama yok) döndürdüğü satır sırasına
göre birini seçer. SQL Server `ORDER BY`'sız sorguda sıra garantisi vermez — index
seçimi veya paralel plan değişince **aynı stajyerin "yöneticisi" ekranda değişebilir**.

**Düzeltme:** Deterministik ikincil anahtar: `.ThenBy(e => e.Id)`. `EmployeeDetailAssembler.cs:183`
bu deseni zaten doğru uyguluyor (`.OrderBy(t => t.Seniority).ThenBy(t => t.FirstName)`) —
tutarlılık için buraya da gerekli.

---

## Bölüm 3 — Karar gerektirenler

### K-1 · `AnnualLeaveDays` override'ı geçmişe dönük çarpılıyor

**Dosya:** `LeaveEntitlement.cs:52-63`
```csharp
if (annualOverride is int perYear)
    return perYear * fullYears;
```

**Somut sayılarla — 16 tam yıllık çalışan (işe giriş 2010-01-01, bugün 2026-08-05):**

| Durum | Hak edilen |
|---|---|
| Override yok (kanuni) | 14×5 + 20×9 + 26×2 = **302** |
| `AnnualLeaveDays = 14` | 14×16 = **224** → 78 gün siliniyor |
| `AnnualLeaveDays = 26` | 26×16 = **416** → 114 gün hediye |

Alanın kendi yorumu (`db/05_full_setup.sql:98-99`) *"yalnızca elle geçersiz kılma
içindir"* diyor, `LeaveEntitlement.cs:49-50` ise *"şirket bu kişiye yılda kaç gün
tanıdığını elle belirlemiştir"*. Model içsel olarak tutarlı. **Risk model değil,
kullanım:** İK personelinin bu alanı "bu yılki kotası" sanıp doldurması çok muhtemel
ve sonuç sessizce geçmişi yeniden yazmak.

**Seçenekler:**
| # | Model | Sonuç |
|---|---|---|
| **A** | Mevcut (kalsın) | Değişiklik yok; ekrana *"Bu değer TÜM geçmiş yıllara uygulanır"* uyarısı şart |
| **B** | Override yalnızca cari yıldan itibaren | `accrued = kanuni(giriş→son yıldönümü) + override × (o günden beri dolan yıl)`; geçmiş korunur, hesap karmaşıklaşır |
| **C** | Override yerine "ek gün" | `accrued = kanuni + bonus`; en anlaşılır, ama "bu kişiye yılda 30 gün" demek imkânsızlaşır |

Öğrenme projesi bağlamında **A + net ekran uyarısı** en düşük maliyetli; kalıcı
sistemde **C** en az sürprizli.

---

### K-2 · Ekranda gösterilen "Kalan izin" ile gerçek talep sınırı farklı

**Dosyalar:** `EmployeeDetailAssembler.cs:99`, `GetAllEmployeesQueryHandler.cs:62`
→ ikisi de `RemainingLeaveDays = accrued − used`
**Gerçek kabul sınırı:** `CreateLeaveRequestCommandHandler.cs:120`
→ `used + requested ≤ accrued + nextGrant`

| Çalışan | Ekranda "Kalan" | Gerçekte talep edebileceği |
|---|---|---|
| 6 aylık, hiç izin kullanmamış | **0** | **14** (avans) |
| 3 yıllık, 42 kullanmış | **0** | **14** |
| 6 aylık, 10 gün kullanmış | **−10** | **4** |

Kullanıcı "0 gün kaldı" görüp talep açmaktan vazgeçiyor ya da "−10" görüp paniğe
kapılıyor. Hesap yanlış değil — gösterim eksik.

**Seçenekler:** (a) DTO'ya `AdvanceLimitDays` (= `nextGrant`) ekleyip ekranda
*"Kalan: 0 · Avans hakkı: +14"* göstermek; (b) tek sayı olarak
`accrued + nextGrant − used` göstermek (basit ama "hak edilmiş" ile "avans"ı
karıştırır); (c) mevcut hâli koruyup yalnızca hata mesajına güvenmek.
**Öneri: (a)** — mevcut hata mesajı (`:122-124`) zaten üç sayıyı da veriyor, ekranın
onunla aynı dili konuşması yeterli.

---

### K-3 · Yönetici aşamasını atlayan hâllerde onay tek kişiye düşüyor

Üç ayrı istisna birleşince "iki aşamalı onay" tek onaya iniyor:
1. Hastalık izni → `PendingHr` doğar (`CreateLeaveRequestCommandHandler.cs:82`)
2. Tepe yönetici (`ManagerId is null`) → `PendingHr` doğar (aynı satır)
3. Talep sahibi HR ise → yönetici onayı yeter (`ApproveLeaveRequestCommandHandler.cs:63-65`)

Üçü de yazılı kullanıcı kararı (2026-07-23 / 2026-08-03) ve her biri tek başına
makul. Ama **1 veya 2 + 3 birleşimi**: `ManagerId`'si olmayan bir İK müdürünün
talebi `PendingHr` doğar ve `EnsureHrStage`'den (`LeaveApprovalGuard.cs:116-126`)
tek bir İK meslektaşının onayıyla geçer. `ManagerApprovedByUserId` null olduğu için
"iki ayrı göz" kuralı hiç devreye girmez.

**Karar:** Bu kabul edilebilir mi, yoksa "hiçbir aşamadan geçmemiş talep için
onaylayan Admin olmalı" mı? İkinci seçenek `EnsureHrStage`'e tek satır:
`if (leaveRequest.ManagerApprovedByUserId is null && actor.Role != Role.Admin) throw ...`

---

### K-4 · Geçmişe dönük talep: türe göre ayrışsın mı?

R-9'un karar tarafı. Hastalık izninde geriye dönüklük **zorunlu** (rapor sonradan
gelir). Yıllık izinde ise "izni kullandım, şimdi kaydını açıyorum" akışı bakiye
disiplinini bozar.

**Seçenekler:** (a) serbest bırak (mevcut); (b) yalnızca `Annual` için
`StartDate >= bugün`; (c) tüm türler için "en fazla N gün geriye" penceresi;
(d) geçmiş tarihli talebi kabul et ama zorunlu olarak İK aşamasına düşür.
**Öneri: (b)** — en az kod, en net kural, hastalık iznini bozmuyor.

---

### K-5 · Görev durumu geçişlerinde durum makinesi yok

**Dosyalar:** `UpdateInternTaskStatusCommandHandler.cs:33`,
`UpdateMyInternTaskStatusCommandHandler.cs:37` — ikisi de:
```csharp
task.Status = (InternTaskStatus)request.NewStatus;
```

Validator yalnızca enum'un tanımlı olduğunu doğruluyor
(`UpdateInternTaskStatusCommandValidator.cs:10-12`). Dolayısıyla `Done → Pending`
geri dönüşü, aynı duruma tekrar yazma, `Pending → Done` atlaması — hepsi serbest.

**Karar soruları:** Stajyer kendi görevini `Done`'dan geri alabilmeli mi? Mentorun
gördüğü `Done`'u stajyer tek başına geri çevirebilmeli mi? Tamamlanma tarihi
(`CompletedAt`) tutulmalı mı?
**Not:** Bu bir hata değil — geri dönüşe izin vermek küçük bir ekipte kasıtlı bir
sadelik olabilir. Ama şu an bu bir *karar* değil, *kararsızlık*: hiçbir yerde
yazmıyor.

---

### K-6 · Mentor ataması hiçbir kuralla denetlenmiyor

**Dosyalar:** `CreateInternCommandHandler.cs:41`, `UpdateInternCommandHandler.cs:41`
— ikisi de `MentorId = request.MentorId`, kontrol yok.

Var olan tek garanti: `FK_Interns_Employees` (mentor gerçekten bir çalışan).
Denetlenmeyenler:
- Mentor **zorunlu değil** → H-1'in kök nedeni
- Mentor **aynı departmandan olmak zorunda değil** — çalışan tarafında bu kural
  `ManagerAssignment.cs:58-60`'ta titizlikle kurulmuş (*"GM hariç aynı departman"*),
  stajyer tarafında karşılığı yok
- Mentor **aktif olmak zorunda değil** (`IsActive` bakılmıyor) — `UnitManagerResolver.cs:29`
  aktiflik süzgecini uyguluyor, mentor ataması uygulamıyor
- Mentor sayısı sınırsız (bir kişi 50 stajyerin mentoru olabilir)

**Karar:** Hangileri kural olsun? Öneri sırası: (1) mentor zorunlu, (2) aktif olmalı,
(3) aynı departman/birim. (1) ve (2) tek satırlık; (3) `ManagerAssignment` deseninin
stajyer versiyonu.

---

### K-7 · Başlamış iznin kalanı geri verilemiyor

**Dosya:** `DeleteLeaveRequestCommandHandler.cs:60-61`
```csharp
if (leaveRequest.StartDate.Date <= DateTime.UtcNow.Date)
    throw new ValidationException("İzin başlamış; artık iptal edilemez veya geri çekilemez.");
```

Gerekçesi yazılı (satır 57-59: *"o günün yerine başkası planlanamayacağı için geri
almanın operasyonel karşılığı kalmamıştır"*) ve ilk gün için ikna edici. Ama
**14 günlük izne 2. günde ara veren** biri kalan 12 günü bakiyeye döndüremiyor.

**Seçenekler:** (a) mevcut (basit, bazı günler yanar); (b) kısmi iptal —
`EndDate`'i bugüne çekip `WorkingDays`'i yeniden hesaplamak (R-12'deki bağı
kurmayı zorunlu kılar); (c) Admin'e "erken dönüş kaydet" yetkisi.
**Not:** (b) seçilirse R-12(a) önce çözülmeli, yoksa bakiye sessizce yanlışa döner.

---

### K-8 · Çalışan ve stajyer e-postaları arasında çapraz benzersizlik

H-4'ün karar tarafı: aynı e-posta hem bir çalışanda hem bir stajyerde bulunabilmeli mi?
Stajyer sonradan çalışan olarak işe alınırsa (yaygın senaryo) iki kayıt aynı e-postayı
taşır. Bu geçiş süreci için **istenen** bir durum olabilir. Karar: e-posta kişi
kimliği mi, yoksa yalnızca iletişim alanı mı?

---

## Bölüm 4 — Test boşlukları (önem sırasıyla)

**Mevcut durum:** 110 test, hepsi yeşil. Kapsam `LeaveEntitlement` saf fonksiyonları,
görünürlük/kırpma kuralları, `ManagerAssignment` saf kuralları, durum seçimi ve
`Delete` akışında **çok iyi**. Boşluklar aşağıda.

### 1. `LeaveApprovalGuard` için tek bir test yok ★★★
Sistemin en kritik kural kümesi (self-onay yasağı, iki ayrı göz, zincir yetkisi,
geçersiz durum geçişi, fail-closed sahip çözümü) **doğrudan hiç test edilmemiş**.
`ApproveLeaveRequestCommandHandlerTests` guard'ı çağırıyor ama her iki testte de
onaylayan **Admin** — yani `EnsureManagerStageAsync:83-84`'te hemen `return`
ediliyor ve gövdenin geri kalanı (satır 88-113) **hiç çalışmıyor**. Test fake'i
bunu itiraf ediyor: `IsInManagerChainAsync => throw new NotImplementedException()`.

Yazılması gereken testler:
- Kendi talebini onaylayamaz / reddedemez (satır 61-62)
- Sahip çözülemezse reddedilir — fail-closed (satır 57-59)
- Zincirde olmayan yönetici `Pending` aşamasında reddedilir (satır 111-113)
- Zincirde olan yönetici geçer; mentor stajyer talebinde geçer (satır 96-107)
- `Approved` / `Rejected` / `Cancelled` durumundaki talep işlenemez (satır 74-75)
- Yönetici onayını veren kişi İK aşamasını işleyemez (satır 123-125)
- HR olmayan biri `PendingHr` aşamasını işleyemez (satır 118-119)

### 2. Yıllık izin bakiye kuralı handler seviyesinde testsiz ★★★
`EnsureAnnualBalanceAsync` (`CreateLeaveRequestCommandHandler.cs:110-125`) hiç
tetiklenmiyor. `LeaveEntitlementTests` bileşenleri ayrı ayrı doğruluyor, ama
`used + requested > accrued + nextGrant` birleşimini test eden **hiçbir test yok**.
Mevcut testler bunu iki katmanda birden atlıyor: hepsi `Type = Unpaid` kullanıyor
(satır 47-49) **ve** fake `GetTotalUsedAnnualDaysAsync => Task.FromResult(0)` dönüyor
(satır 104).

Yazılması gereken: sınırda geçen talep, sınırı 1 gün aşan talep, bekleyen taleplerin
"kullanılan"a sayıldığı senaryo, `AnnualLeaveDays` override'lı çalışan.

### 3. Çakışma kontrolü testsiz ★★★
`HasOverlapAsync` fake'i sabit `false` dönüyor (satır 101-102) → `:50-52`'deki kural
hiç çalışmıyor. En az bir test: çakışan tarih → `ValidationException`.

### 4. Stajyer + izin kuralları tamamen testsiz ★★
- Stajyer `Annual` talep edemez (`:59-61`)
- Süresi dolmuş staj (`:46-47`)
- Pasif çalışan (`:41-42`)
- Hesabı hiçbir kayda bağlı olmayan kullanıcı (`:37-39`)

Mevcut `FakeInternRepository` `GetByUserIdAsync => null` döndüğü için stajyer dalı
`CreateLeaveRequestCommandHandlerTests`'te **hiç çalışmıyor** (satır 141'deki yorum
bunu açıkça söylüyor).

### 5. `RejectLeaveRequestCommandHandler` için hiç test yok ★★
Onaylamanın simetriği; guard'ı paylaşıyor ama kendi durum yazma davranışı
(`Status = Rejected` + red alanları) hiç doğrulanmamış.

### 6. `EnsureBalanceStillSufficientAsync` testsiz ★★
"Kontrol zamanı ≠ kullanım zamanı" savunması (`ApproveLeaveRequestCommandHandler.cs:99-116`).
R-1'deki hata (bu denetimin `PendingHr` dalında hiç çalışmaması) **bir testle
yakalanırdı**.

### 7. `UpdateEmployee` döngü önleme testsiz ★★
`EnsureManagerAssignableAsync:188-190` — A→B→A döngüsünü engelleyen tek kural.
`ManagerAssignmentTests` yalnızca saf `ManagerAssignment` kurallarını test ediyor;
`IsInManagerChainAsync` çağrısını içeren handler yolu hiç çalışmıyor.
Ayrıca "kendi yöneticisi olamaz" (satır 174-175) de testsiz.

### 8. `EnsureSubordinatesRemainValidAsync` handler seviyesinde testsiz ★
Saf kural test edilmiş (`ManagerAssignmentTests: gm_dusurulunce_bagli_gmy_gecersiz_kalir`,
`mudur_departman_degistirince_eski_astlari_gecersiz_kalir` — bu ikisi çok iyi), ama
handler'ın bunu **hangi koşulda çağırdığı** (`:66` — yalnız Seniority veya
DepartmentId değişince) test edilmemiş.

### 9. `AccountRequests` Approve/Reject testsiz ★
Durum geçişi (`Pending` dışı reddedilir), "bu arada hesap açılmış" kontrolü
(`EnsureSubjectStillNeedsAccountAsync`), rol override mantığı (`Role ?? SuggestedRole`) —
hiçbiri test edilmemiş.

### 10. Hastalık izninde rapor zorunluluğu testsiz ★
Ne validator testinde (`CreateLeaveRequestCommandValidatorTests` 6 test — hiçbiri
`MedicalReport` ile ilgili değil) ne handler testinde. Kural iki yerde birden
tanımlı (`Validator:35-37` ve `Handler:67-69`); ikisinin de çalıştığını gösteren
test yok.

### 11. `workingDays == 0` reddi handler seviyesinde testsiz ★
`WorkingDays` saf fonksiyonu hafta sonu senaryolarını test ediyor (7 vaka, çok iyi),
ama handler'ın buna verdiği tepki (`:73-74`, *"Seçilen aralıkta iş günü yok"*)
test edilmemiş.

### 12. `AccountRoleResolver` testsiz
İki satır, ama hem otomatik hem manuel hesap akışında rol türetmenin **tek kaynağı**.
`IsManagerial()` sınırının (Müdür → Manager, Müdür Yrd. → Employee) doğru olduğunu
gösteren tek bir test yok.

### 13. `UnitAssignment.EnsureUnitInDepartmentAsync` testsiz
5 handler'ın paylaştığı kural (Create/Update × Employee/Intern). Saf değil (repo
alıyor) ama tek bir fake ile kolayca test edilebilir.

### 14. `UpdateInternTaskStatus` (mentor yolu) testsiz
`UpdateMyInternTaskStatusCommandHandlerTests` yalnızca stajyerin kendi yolunu test
ediyor. Mentor yolu (`MentorshipGuard.EnsureMentorAsync` üzerinden) test edilmemiş —
`MentorshipGuardTests` guard'ı ayrıca test ediyor, o yüzden bu en düşük öncelikli.

**Kapsanmayan alanların ortak paydası:** Mevcut testler durum **seçimini** ve **saf
hesabı** çok iyi kapsıyor; kapsanmayan şey **reddetme kuralları** — yani bir talebin
hangi durumda geçmemesi gerektiği. 110 testin neredeyse tamamı "doğru girdi doğru
sonucu üretiyor mu" sorusunu soruyor; "yanlış girdi gerçekten reddediliyor mu"
sorusu izin modülünde büyük ölçüde açıkta.

---

## Bölüm 5 — İyi çözülmüş kurallar

Bunlar denetimde **doğrulandı** ve öğrenme projesi için örnek niteliğinde:

1. **Kıdem kademeleri İş Kanunu md. 53 ile birebir doğru.** En sık yapılan iki hata
   (5. yılı üst kademeye koymak, 15. yılı alt kademede bırakmak) hem kodda hem testte
   doğru — `LeaveEntitlementTests.cs:46,49` bunları açıkça hedef alıyor.

2. **Yarı açık aralık `[start, end)` semantiği DÖRT ayrı katmanda tutarlı:**
   `LeaveEntitlement.WorkingDays:92`, `HasOverlapAsync` SQL'i (`LeaveRequestRepository.cs:121-122`),
   `db/18_sp_hr_dashboard.sql:95,136`, WebUI `Overlaps` (`LeaveRequestsController.cs:522-524`).
   Böyle bir semantiğin dört yerde birden doğru tutulması nadirdir.

3. **Anlam değişimi düzgün taşınmış.** `db/19_leave_enddate_isbasi.sql` yalnızca veriyi
   kaydırmakla kalmıyor: `WorkingDays`'e neden dokunulmadığını matematiksel olarak
   gerekçelendiriyor (eski kapalı aralık + kaydırma = yeni yarı açık aralık, aynı gün
   kümesi — **doğrulandı, gerekçe geçerli**) ve extended property damgasıyla ikinci
   kez çalışmayı engelliyor. Bu, üretim seviyesi bir migration disiplinidir.

4. **Bekleyen talepler "kullanılan"a dahil.** `GetTotalUsedAnnualDaysAsync:279-284`
   `Pending`, `PendingHr`, `Approved` durumlarını birlikte sayıyor. Handler yorumu
   (`:107-109`) nedenini yazıyor: *"yoksa dört bekleyen talep ayrı ayrı kontrolü
   geçip hakkı katlardı"*. Bu, bakiye kuralında en sık yapılan hatadır ve burada
   doğru çözülmüş.

5. **Fail-closed sahip çözümü.** `LeaveApprovalGuard.cs:57-59` — sahip çözülemezse
   işlem reddediliyor. Yorumu (`:54-56`) tuzağı adıyla anıyor: `int? == int`
   karşılaştırması null'da sessizce `false` döner ve self-onay kilidi **fark
   edilmeden** devre dışı kalırdı. Bu inceliği görmüş olmak dikkate değer.

6. **"İki ayrı göz" için tek `ReviewedBy` yerine iki ayrı sütun çifti.**
   `db/05_full_setup.sql:163-164` gerekçesini şemanın içine yazmış:
   *"tek bir 'ReviewedBy' alanı 'aynı kişi iki aşamayı da onaylamasın' kuralını
   denetlemeye yetmezdi"*. Kuralın veri modeline yansıtılması doğru yaklaşım.

7. **Onay anında bakiye yeniden denetleniyor.** *"Kontrol zamanı ≠ kullanım zamanı"*
   (`ApproveLeaveRequestCommandHandler.cs:52-54`) — TOCTOU'yu kavramış bir savunma.
   Koşulun `used > limit` olması (talebi tekrar eklemeden) da doğru: talep zaten
   "kullanılan"ın içinde. R-1 bunun kapsamıyla ilgili, doğruluğuyla değil.

8. **Okuma yolu ile yazma yolu simetrik.** `GetPendingApprovalsQueryHandler:57-69`
   ile `LeaveApprovalGuard`'ın kuralları **tek tek karşılaştırıldı ve gerçekten
   örtüşüyor**: Admin muafiyeti, ekip/zincir, mentor dalı, iki-göz kuralı, kendi
   talebi hariç. Kullanıcının işleyemeyeceği bir talebi listede görmemesi (ve tersi)
   sağlanmış.

9. **Zincir sorgularında derinlik sigortası.** `IsInManagerChainAsync` ve `GetTeamAsync`
   (`EmployeeRepository.cs:190,219`) ikisi de `Depth < 32` ile sonsuz döngüye karşı
   korunmuş — üstelik **aynı sınırla**, ters yönlerde. Yorumlar gerekçeyi yazıyor.

10. **Yönetici değişikliğinde astların korunması.** `EnsureSubordinatesRemainValidAsync`
    (`UpdateEmployeeCommandHandler.cs:139-168`) tek yönlü işleyen bir kuralın org'u
    sessizce bozabileceğini görmüş, ve **otomatik taşıma yerine engellemeyi** seçmiş
    (satır 135-137: *"astları sessizce başka bir yöneticiye bağlamak, İK'nın görmediği
    bir org değişikliği yapmak olurdu"*). Doğru tercih.

11. **Rol JWT claim'inden değil DB'den okunuyor.** `ApproveLeaveRequestCommandHandler.cs:82`
    ve `EmployeeDetailAssembler.cs:117` — *"claim bayatlayabilir"*. İzin akışında
    rol değişikliğinin anında etkili olması doğru davranış.

12. **Çok tabloya yazan iki yer gerçek transaction kullanıyor.**
    `UserRepository.CreateForPersonAsync:106-131` (Users + Employees/Interns + AccountRequests)
    ve `EmployeeRepository.DeleteWithAccountAsync:88-114`. İkisi de try/catch/rollback ile.

13. **Hesap silinmiyor, pasife alınıyor.** `EmployeeRepository.cs:97-98` gerekçesi:
    *"O hesap başka talepleri (RequestedBy/ReviewedBy) referanslıyor olabilir;
    hard-delete FK'ye takılır ve denetim izini bozar."* `DeleteUserCommandHandler:30-39`
    da aynı ilkeyi uyguluyor.

14. **`AccountRequests`'te "bir kişiye tek bekleyen talep" DB seviyesinde garanti.**
    Filtered unique index (`db/06_account_requests.sql:52-58`). Uygulama katmanına
    bırakılmamış — R-4'te eksikliği hissedilen desen burada doğru uygulanmış.

15. **Enum sayıları SQL'e gömülmemiş, `@Today` parametre olarak geçiliyor.**
    `db/18_sp_hr_dashboard.sql:25,33` — GETDATE() yerine uygulamanın UTC "bugün"ünü
    parametre alması hem test edilebilirlik hem zaman tutarlılığı açısından doğru.
    (H-5'teki sorun bu disiplinin uygulama tarafında bir yerde kaçmasından kaynaklanıyor —
    DB tarafı örnek davranıyor.)

---

## Ek: Öncelik sırasıyla eylem listesi

| # | Bulgu | Efor | Etki |
|---|---|---|---|
| 1 | H-1 — mentorsuz stajyer kilidi | 1 satır | Akış tıkanması çözülür |
| 2 | R-1 — bakiye yeniden denetimini `switch` dışına al | 3 satır | Tepe yöneticilerde bakiye ağı kurulur |
| 3 | Bölüm 4 #1 — `LeaveApprovalGuard` testleri | Orta | En kritik kuralları kilitler |
| 4 | R-6 — koşullu `UPDATE ... WHERE Status = @Expected` | Küçük | İki yarış durumunu birden kapatır |
| 5 | H-5 — `GetOrganizationQueryHandler:74` → UTC | 1 satır | Katmanlar arası çelişki azalır |
| 6 | H-2 — sınıf yorumunu düzelt | 1 satır | Gelecekteki sessiz hatayı önler |
| 7 | R-4 — `UX_Employees_UserId` / `UX_Interns_UserId` | 2 satır SQL | Kimlik belirsizliğini DB'de kapatır |
| 8 | Bölüm 4 #2/#3 — bakiye + çakışma testleri | Orta | En büyük test boşluğu |
| 9 | R-8 — izin süresi üst sınırı | 3 satır | Kontrolsüz döngüyü kapatır |
| 10 | H-3/H-4 — T.C. ve stajyer e-posta benzersizliği | Orta | Veri bütünlüğü |
