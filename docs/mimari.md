# Mimari Dokümanı

> Teslimat §7.3 — *Mimari doküman (kısa): katmanlar ne işe yarıyor, hangi projede
> ne var, örnek akış ("Çalışan izin talebi nasıl işleniyor?")*

Teknoloji **seçimlerinin gerekçeleri** (neden Dapper, neden MediatR, alternatifleri,
ödünleri) ayrı bir belgededir: [`HRManagement_Teknoloji_Rehberi.pdf`](HRManagement_Teknoloji_Rehberi.pdf).
Bu doküman **yapıyı** anlatır.

---

## 1. Genel bakış

Uygulama **Clean Architecture** ile beş projeye bölünmüştür. Temel ilke:

> **Bağımlılıklar içeri doğru akar; iş kuralları merkezdedir ve hiçbir
> framework'e bağlı değildir.**

```
        ┌─────────────────────────────────────────────┐
        │                  Domain                     │   entity + enum
        │        (hiçbir dış bağımlılık yok)          │
        └────────────────────▲────────────────────────┘
                             │
        ┌────────────────────┴────────────────────────┐
        │                Application                  │   use-case + iş kuralı
        │   MediatR · FluentValidation · arayüzler    │   + repository ARAYÜZLERİ
        └──────▲──────────────────────────────▲───────┘
               │                              │
   ┌───────────┴──────────┐      ┌────────────┴─────────────┐
   │    Infrastructure    │      │           API            │
   │  Dapper · JWT · hash │      │  controller + JWT doğrul.│
   └───────────▲──────────┘      └────────────▲─────────────┘
               │                              │
               └───────── API (composition root) 
                                              ⋮  HTTP  (proje referansı YOK)
                                       ┌──────┴───────┐
                                       │    WebUI     │   MVC + Refit
                                       └──────────────┘
```

**Bu kuralı doküman değil, derleyici korur.** WebUI'nin `.csproj` dosyasında
Application referansı yoktur; bir controller'da `using HRManagement.Application`
yazılırsa proje **derlenmez**.

---

## 2. Katmanlar — hangi projede ne var

### 2.1. `HRManagement.Domain`

| | |
|---|---|
| **Sorumluluğu** | Sistemin ortak dili: kavramlar ve sabit sınıflandırmalar |
| **İçinde ne var** | `Entities/` — `Employee`, `Intern`, `LeaveRequest`, `Department`, `Unit`, `User`, `EmployeeNote`, `InternNote`, `InternTask`, `AccountRequest`<br>`Enums/` — `Role`, `LeaveType`, `LeaveStatus`, `SeniorityLevel`, `Gender`, `InternTaskStatus`, `AccountRequestStatus` |
| **Ne YOK** | Hiçbir NuGet paketi. Veritabanı, HTTP, framework bilgisi yok. |
| **Bağımlılığı** | **Hiçbir şey** |

Entity'ler veri taşıyıcıdır (anemik). Kurallar Application'daki adlandırılmış
kural sınıflarında toplanmıştır — gerekçe ve tartışma teknoloji rehberi Bölüm 9'da.

### 2.2. `HRManagement.Application`

| | |
|---|---|
| **Sorumluluğu** | Use-case'ler ve **tüm iş kuralları** |
| **İçinde ne var** | `Features/{Modül}/{Commands\|Queries}/{Operasyon}/` — her use-case 3 dosya: Command/Query + Handler + Validator<br>`Features/{Modül}/Shared/` — paylaşılan kural sınıfları<br>`Interfaces/` — repository ve servis **arayüzleri** (`IEmployeeRepository`, `IJwtTokenGenerator`, `IPasswordHasher`…)<br>`Behaviors/ValidationBehavior.cs` — MediatR boru hattı halkası<br>`dto/` — katman dışına çıkan veri nesneleri<br>`Mapping/` — entity → DTO dönüşümleri<br>`Services/LeaveEntitlement.cs` — saf izin hakkı hesabı |
| **Ne YOK** | SQL, HttpContext, controller, view |
| **Bağımlılığı** | Yalnızca `Domain` |

Modüller: `AccountRequests`, `Dashboard`, `Departments`, `Employees`, `Interns`,
`LeaveRequests`, `Units`, `Users`.

**Paylaşılan kural sınıfları** (birden çok use-case'in ortak kuralı — "domain service"):

| Sınıf | Kuralı |
|---|---|
| `LeaveApprovalGuard` | Kim hangi izin talebini onaylayabilir/reddedebilir ("onaylayabilen reddedebilir" simetrisi) |
| `EmployeeVisibility` | Kim hangi çalışanı görebilir (liste ve detay ortak kullanır) |
| `EmployeeDetailAssembler` | Detay DTO'sunu derler ve hassas alanları istekçiye göre kırpar |
| `MentorshipGuard` | "Bu kişi bu stajyerin mentoru mu?" |
| `UnitManagerResolver` | Stajyerin türetilmiş yöneticisi (birim → departman) |
| `ManagerAssignment` | Yönetici atama kuralları |

### 2.3. `HRManagement.Infrastructure`

| | |
|---|---|
| **Sorumluluğu** | Application'ın tanımladığı arayüzlerin **teknoloji ile gerçeklenmesi** |
| **İçinde ne var** | `Persistence/` — Dapper repository'leri + `DbConnectionFactory`<br>`Security/JwtTokenGenerator.cs` — token **üretimi**<br>`Security/PasswordHasher.cs` — BCrypt |
| **Ne YOK** | İş kuralı. Repository'ler yalnızca veri okur/yazar. |
| **Bağımlılığı** | `Domain` + `Application` |

### 2.4. `HRManagement.API`

| | |
|---|---|
| **Sorumluluğu** | HTTP sınırı, kimlik doğrulama, yetkilendirme — ve **composition root** |
| **İçinde ne var** | `Controllers/` — 8 controller<br>`Models/` — request/response modelleri + `BaseResponse<T>`<br>`Middleware/` — `GlobalExceptionHandler`, `UseBaseResponseStatusCodes`<br>`Seeding/AdminSeeder.cs` — ilk Admin hesabı<br>`DependencyInjection.cs` — servis kayıtları + JWT **doğrulama**<br>`Program.cs` — ne kurulduğu ve middleware sırası |
| **Bağımlılığı** | `Application` + `Infrastructure` |

Controller **incedir**: request'i Command/Query'ye çevirir, `ISender.Send` eder,
sonucu response modeline çevirir. İş mantığı controller'a yazılmaz.

### 2.5. `HRManagement.WebUI`

| | |
|---|---|
| **Sorumluluğu** | Kullanıcı arayüzü |
| **İçinde ne var** | `Controllers/` + `Views/` — MVC<br>`Services/IXxxApi.cs` — **Refit** arayüzleri (API sözleşmesi)<br>`Services/BearerTokenHandler.cs` — her isteğe JWT ekler<br>`Models/Api/` — API yanıt modelleri<br>`Models/` — ViewModel'ler ve gösterim yardımcıları |
| **Ne YOK** | İş katmanına referans. SQL. Domain tipi. |
| **Bağımlılığı** | **Hiçbir iş katmanı** — API ile yalnızca HTTP |

WebUI'deki her kontrol (form doğrulaması, role göre menü gizleme) **kullanıcı
deneyimi** içindir. Otorite her zaman API + Application'dadır.

---

## 3. Katmanlar arası iletişim

```
Controller (API)  →  ISender.Send(Command/Query)
                  →  MediatR boru hattı (ValidationBehavior)
                  →  Handler (Application)  ← iş kuralları burada
                  →  IXxxRepository (Application'da tanımlı arayüz)
                  →  XxxRepository (Infrastructure, Dapper)
                  →  SQL Server
```

- Repository'ler **Application'da tanımlanan arayüzler** üzerinden kullanılır.
  Somut sınıflar yalnızca `Program.cs`'te (composition root) bağlanır.
- `CancellationToken` handler imzasından repository'ye kadar iletilir, hiçbir
  katmanda yutulmaz.
- Domain entity'si veya Application Command/Query tipi API yanıtına **sızmaz**.

---

## 4. Örnek akış — "Çalışan izin talebi nasıl işleniyor?"

Bir çalışanın 5 günlük yıllık izin talebi oluşturmasının tam yolu.

### 4.1. Adım adım

**1 — Tarayıcı → WebUI**
Kullanıcı `/LeaveRequests/Create` formunu doldurup gönderir. İstek, oturum
cookie'siyle birlikte WebUI'ye ulaşır. Cookie doğrulanır, kullanıcının kimliği
belirlenir.

**2 — WebUI controller**
`LeaveRequestsController` ViewModel'i API request modeline çevirir ve
`ILeaveRequestApi.CreateAsync(...)` çağırır. Bu bir **Refit** arayüzüdür;
gerçek HTTP kodunu Refit üretir — elle `HttpClient`/JSON kodu yazılmaz.

**3 — BearerTokenHandler**
Bir `DelegatingHandler` olarak zincire takılıdır. Cookie ticket'ının **içinde**
saklanan JWT'yi okur ve isteğe `Authorization: Bearer <token>` başlığını ekler.
Token tarayıcıya hiç verilmez (JavaScript okuyamaz, `localStorage`'a yazılmaz).

**4 — API middleware boru hattı**
```
UseExceptionHandler → UseBaseResponseStatusCodes → UseAuthentication → UseAuthorization
```
`UseAuthentication` "sen kimsin?" sorusunu cevaplar (JWT imzası, issuer, audience
ve süre doğrulanır); `UseAuthorization` "yetkin var mı?" sorusunu **dolu kimlik
üzerinden** cevaplar. Sıra tersine çevrilirse yetki kontrolü daima boş kimlikle
çalışır. Ayrıca **global fallback policy** yüzünden üzerinde `[Authorize]`
yazmayan uçlar da kimlik doğrulaması ister — uçlar "kilitli doğar".

**5 — API controller**
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateLeaveRequestRequest request)
{
    var id = await _mediator.Send(new CreateLeaveRequestCommand(
        CurrentUserId(),          // ← imzalı JWT claim'inden, gövdeden DEĞİL
        request.Type, request.StartDate, request.EndDate,
        request.Description, request.MedicalReport));
    ...
}
```
**Kritik nokta:** talep sahibinin kimliği istek gövdesinden **asla** alınmaz.
Gövde istemcinin elindedir; "ben başkasıyım" diyebilirdi. Kimlik yalnızca
imzalı token'dan okunur.

**6 — MediatR boru hattı → ValidationBehavior**
Mesaj handler'a ulaşmadan önce `CreateLeaveRequestCommandValidator` çalışır.
Burada yalnızca **veritabanına bakmadan** cevaplanabilen kurallar denetlenir:

- Bitiş (işe başlama) tarihi > başlangıç tarihi — bitiş günü izne dahil değildir
- İzin türü geçerli bir enum değeri
- Açıklama ≤ 500 karakter
- Hastalık izniyse rapor bilgisi dolu

Hata varsa handler **hiç çağrılmaz**; `ValidationException` fırlar.

**7 — Handler → iş kuralları**
`CreateLeaveRequestCommandHandler` sırayla, hepsi veritabanına bakarak:

1. **Kimlik çözümü** — hesap önce çalışan, yoksa stajyer kaydına çözülür. İkisi de yoksa red.
2. **Aktiflik** — pasif çalışan talep açamaz; süresi dolmuş staj için talep açılamaz.
3. **Tarih çakışması** — kişinin bekleyen/onaylı başka bir talebiyle kesişiyor mu.
4. **Yıllık izin hakkı** (yalnızca `Annual` için):
   `kullanılan + talep ≤ hak edilen + bir sonraki yılın hakkı`
   "Kullanılan"a **bekleyen** talepler de dahildir — her talep yerini baştan
   rezerve eder; yoksa dört bekleyen talep ayrı ayrı kontrolü geçip hakkı katlardı.
   Stajyerler yıllık izin biriktirmez, ücretsiz izin kullanır.
5. **İş günü hesabı** — hafta sonu hariç; sonuç sıfırsa red.
6. **Başlangıç durumu** — hastalık izni yönetici aşamasını **atlar**, doğrudan İK
   onayına düşer (`PendingHr`); diğerleri `Pending` başlar.

**8 — Repository → veritabanı**
```csharp
const string sql = @"
    INSERT INTO LeaveRequests (EmployeeId, InternId, Type, StartDate, ...)
    VALUES (@EmployeeId, @InternId, @Type, @StartDate, ...);
    SELECT CAST(SCOPE_IDENTITY() AS INT);";
```
Tüm değerler **parametre** olarak geçer; kullanıcı girdisi hiçbir zaman SQL
metnine birleştirilmez (SQL injection koruması).

**9 — Geri dönüş**
Yeni kaydın Id'si `BaseResponse<int>.Success(id)` zarfıyla JSON'a çevrilir.
Refit bunu okur; WebUI `IsSuccess` alanına bakar, başarılıysa listeye
yönlendirir, değilse `Message`'ı kullanıcıya gösterir.

### 4.2. Akış diyagramı

```
TARAYICI ──cookie──► WebUI Controller
                        │ ViewModel → API request
                        ▼
                     ILeaveRequestApi (Refit)
                        ▼
                     BearerTokenHandler ── JWT ekle
                        ▼ ───── HTTP ─────
                     API middleware (exception → status → authn → authz)
                        ▼
                     LeaveRequestsController ── userId = JWT claim
                        ▼ _mediator.Send(CreateLeaveRequestCommand)
                     ValidationBehavior ── input kuralları
                        ▼
                     CreateLeaveRequestCommandHandler ── İŞ KURALLARI
                        ▼ ILeaveRequestRepository.AddAsync
                     LeaveRequestRepository (Dapper)
                        ▼
                     SQL SERVER
                        │
                        ◄── Id ──► BaseResponse<int> ──► JSON ──► WebUI
```

### 4.3. Devamı — onay akışı

```
        oluşturuldu
             │
   ┌─────────┴──────────┐
   │                    │  (hastalık izni)
   ▼                    ▼
Pending             PendingHr
(yönetici onayı)    (İK onayı)
   │                    │
   │ yönetici onaylar   │ İK onaylar
   ├──────► PendingHr ──┴──────► Approved
   │
   └──────► Rejected  (her iki aşamada da mümkün)
```

Özel kural: talep sahibi **İK rolündeyse** yönetici onayı yeterlidir, İK aşaması
atlanır (kişi kendi talebini onaylamasın diye). Denetim izi ayrı kolonlarda
tutulur: `ManagerApprovedByUserId` / `HrApprovedByUserId` / `RejectedByUserId`
ve zaman damgaları.

---

## 5. Kesişen endişeler (cross-cutting)

| Endişe | Nerede | Mekanizma |
|---|---|---|
| Input doğrulama | Application | MediatR `ValidationBehavior` |
| İş kuralı reddi → 400 | API | `GlobalExceptionHandler` |
| Gövdesiz hata yanıtları (401/403/404) | API | `UseBaseResponseStatusCodes` |
| Model binding hataları | API | `ApiBehaviorOptions.InvalidModelStateResponseFactory` |
| Kimlik doğrulama | API / WebUI | JWT Bearer / Cookie |
| Yetkilendirme varsayılanı | API / WebUI | Global fallback policy / `AuthorizeFilter` |
| Token taşıma | WebUI | `BearerTokenHandler` |
| Kültür ve tarih biçimi | WebUI | `UseRequestLocalization` (tr-TR) |

Bu üç mekanizma birlikte **tüm** API yanıtlarının aynı `BaseResponse<T>` zarfında
dönmesini garanti eder — başarı da hata da. İstemci tek tip deserialize eder.

**Bilinen eksikler:** merkezî loglama ve transaction yönetimi henüz yok. İkisi de
mevcut yapıya birer MediatR behavior'ı (`LoggingBehavior`, `TransactionBehavior`)
eklenerek kapatılabilir — ayrıntı teknoloji rehberi Bölüm 10'da.

---

## 6. Yetkilendirme modeli

Rol **kaba bir kapıdır**; asıl yetki **ilişkiden** gelir.

| Katman | Ne yapar | Örnek |
|---|---|---|
| API | Rol kapısı | `[Authorize(Roles = "HR,Admin")]` |
| Application | İlişki kontrolü | Yönetici yalnızca `ManagerId` zinciriyle kendisine bağlı çalışanları görür; mentor yalnızca kendi stajyerini |
| WebUI | Görünürlük | Menü ve buton gizleme — **yalnızca UX**, güvenlik değil |

Bu yüzden bir uç hem rol etiketi taşır hem de içeriği kişiye göre süzer.
WebUI tamamen atlanıp API'ye doğrudan istek atılsa bile güvenlik aynı kalır.

---

## 7. İlgili dokümanlar

- [`../README.md`](../README.md) — kurulum ve çalıştırma
- [`veri-modeli.md`](veri-modeli.md) — ER diyagramı ve veri sözlüğü
- [`HRManagement_Teknoloji_Rehberi.pdf`](HRManagement_Teknoloji_Rehberi.pdf) — teknoloji kararlarının gerekçeleri
- [`../db/README.md`](../db/README.md) — SQL script'leri
- [`../CLAUDE.md`](../CLAUDE.md) — geliştirme kuralları
