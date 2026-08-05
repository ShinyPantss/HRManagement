# Mimari Dokümanı

> Teslimat §7.3 — *Mimari doküman (kısa): katmanlar ne işe yarıyor, hangi projede
> ne var, örnek akış ("Çalışan izin talebi nasıl işleniyor?")*

Teknoloji **seçimlerinin gerekçeleri** (neden Dapper, neden MediatR, alternatifleri,
ödünleri) ayrı bir belgededir: [`docs/teknoloji-secimleri.md`](docs/teknoloji-secimleri.md).
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
| **İçinde ne var** | `Features/{Modül}/{Commands\|Queries}/{Operasyon}/` — her use-case 3 dosya: Command/Query + Handler + Validator<br>`Features/{Modül}/Shared/` — paylaşılan kural sınıfları<br>`Interfaces/` — repository ve servis **arayüzleri** (`IEmployeeRepository`, `IJwtTokenGenerator`, `IPasswordHasher`, `IAiAssistant`, `ISqlQueryRunner`, `IConversationStore`…)<br>`Behaviors/ValidationBehavior.cs` — MediatR boru hattı halkası<br>`dto/` — katman dışına çıkan veri nesneleri<br>`Mapping/` — entity → DTO dönüşümleri<br>`Services/LeaveEntitlement.cs` — saf izin hakkı hesabı |
| **Ne YOK** | Çalıştırılan SQL, HttpContext, controller, view |
| **Bağımlılığı** | Yalnızca `Domain` |

Modüller (10): `AccountRequests`, `Assistant`, `Dashboard`, `Departments`, `Employees`,
`Interns`, `LeaveRequests`, `Organization`, `Units`, `Users` — toplam 48 handler, 23 validator.

**Paylaşılan kural sınıfları** (birden çok use-case'in ortak kuralı — "domain service"):

| Sınıf | Kuralı |
|---|---|
| `LeaveApprovalGuard` | Kim hangi izin talebini onaylayabilir/reddedebilir ("onaylayabilen reddedebilir" simetrisi) |
| `EmployeeVisibility` | Kim hangi çalışanı görebilir (liste ve detay ortak kullanır) |
| `EmployeeDetailAssembler` | Detay DTO'sunu derler ve hassas alanları istekçiye göre kırpar |
| `MentorshipGuard` | "Bu kişi bu stajyerin mentoru mu?" |
| `UnitManagerResolver` | Stajyerin türetilmiş yöneticisi (birim → departman) |
| `ManagerAssignment` | Yönetici atama kuralları |
| `UnitAssignment` | Birim–departman tutarlılığı (birim, kişinin departmanına ait olmalı) |
| `AccountRoleResolver` | Hesap rolünü kıdemden türetir |
| `SqlReadOnlyGuard` | Asistanın ürettiği SQL salt okuma mu (bkz. §5) |
| `HrDatabaseSchema` | Asistanın sistem istemi: şema, enum karşılıkları, tuzaklar (bkz. §5) |

> **Not — "Application'da SQL yok" kuralının bilinçli istisnası:** asistan modülü
> Application'da SQL *metni* barındırır (sistem istemindeki şema açıklaması ve
> `SqlReadOnlyGuard`'ın anahtar kelime listesi). SQL'i **çalıştıran** kod hâlâ
> Infrastructure'dadır (`ISqlQueryRunner` arayüzünün arkasında); Application
> projesinde ne Dapper referansı ne de bir bağlantı nesnesi vardır. Denetim
> kuralının Application'da durmasının sebebi DB'siz birim testine açık olmasıdır.

### 2.3. `HRManagement.Infrastructure`

| | |
|---|---|
| **Sorumluluğu** | Application'ın tanımladığı arayüzlerin **teknoloji ile gerçeklenmesi** |
| **İçinde ne var** | `Persistence/` — Dapper repository'leri + `DbConnectionFactory` + `ReadOnlySqlQueryRunner`<br>`Security/JwtTokenGenerator.cs` — token **üretimi**<br>`Security/PasswordHasher.cs` — BCrypt<br>`Ai/ClaudeAssistant.cs` — Anthropic SDK ile model çağrısı ve araç döngüsü<br>`Ai/MemoryConversationStore.cs` — asistan konuşma geçmişi (bellek içi) |
| **Ne YOK** | İş kuralı. Repository'ler yalnızca veri okur/yazar. |
| **Bağımlılığı** | `Domain` + `Application` |

`DbConnectionFactory` iki bağlantı üretir: normal (`DefaultConnection`) ve asistanın
kullandığı **salt okuma** bağlantısı (`ReadOnlyConnection`). İkincisi *tembel* çözülür —
asistan yapılandırılmamışsa uygulamanın geri kalanı çalışmaya devam eder.

### 2.4. `HRManagement.API`

| | |
|---|---|
| **Sorumluluğu** | HTTP sınırı, kimlik doğrulama, yetkilendirme — ve **composition root** |
| **İçinde ne var** | `Controllers/` — 11 controller: `AccountRequests`, `Assistant`, `Auth`, `Dashboard`, `Departments`, `Employees`, `Interns`, `LeaveRequests`, `Organization`, `Units`, `Users`<br>`Models/` — request/response modelleri + `BaseResponse<T>`<br>`Middleware/` — `GlobalExceptionHandler`, `UseBaseResponseStatusCodes`<br>`Seeding/AdminSeeder.cs` — ilk Admin hesabı<br>`DependencyInjection.cs` — servis kayıtları + JWT **doğrulama**<br>`Program.cs` — ne kurulduğu ve middleware sırası |
| **Bağımlılığı** | `Application` + `Infrastructure` |

Controller **incedir**: request'i Command/Query'ye çevirir, `IMediator.Send` eder,
sonucu response modeline çevirir. İş mantığı controller'a yazılmaz.

### 2.5. `HRManagement.WebUI`

| | |
|---|---|
| **Sorumluluğu** | Kullanıcı arayüzü |
| **İçinde ne var** | `Controllers/` (13) + `Views/` — MVC<br>`Services/IXxxApi.cs` — 12 **Refit** arayüzü (API sözleşmesi)<br>`Services/BearerTokenHandler.cs` — her isteğe JWT ekler<br>`ViewComponents/EmployeeCountViewComponent.cs` — kenar çubuğundaki çalışan sayısı rozeti (5 dk önbellek)<br>`Views/Shared/_AssistantWidget.cshtml` — asistan sohbet penceresi (yalnız İK/Admin)<br>`Models/Api/` — API yanıt modelleri<br>`Models/` — ViewModel'ler ve gösterim yardımcıları |
| **Ne YOK** | İş katmanına referans. SQL. Domain tipi. |
| **Bağımlılığı** | **Hiçbir iş katmanı** — API ile yalnızca HTTP |

WebUI'deki her kontrol (form doğrulaması, role göre menü gizleme) **kullanıcı
deneyimi** içindir. Otorite her zaman API + Application'dadır.

---

## 3. Katmanlar arası iletişim

```
Controller (API)  →  IMediator.Send(Command/Query)
                  →  MediatR boru hattı (ValidationBehavior)
                  →  Handler (Application)  ← iş kuralları burada
                  →  IXxxRepository (Application'da tanımlı arayüz)
                  →  XxxRepository (Infrastructure, Dapper)
                  →  SQL Server
```

- Repository'ler **Application'da tanımlanan arayüzler** üzerinden kullanılır.
  Somut sınıflar yalnızca `Program.cs`'te (composition root) bağlanır.
- Domain entity'si veya Application Command/Query tipi API yanıtına **sızmaz**.

> **`CancellationToken` zinciri şu an eksiktir (bilinen borç).** Token her handler
> imzasında var ve `ValidationBehavior` onu ileri taşıyor; ancak (1) API
> controller'ları `CancellationToken` parametresi almıyor, dolayısıyla boru hattına
> HTTP'den hiç girmiyor; (2) repository **arayüzleri** token parametresi
> tanımlamıyor; (3) Dapper çağrılarında `CommandDefinition` kullanılmıyor. Uçtan uca
> iletildiği tek yol asistan yoludur (`ISqlQueryRunner` → `CommandDefinition`).
> Sonuç: istemci bağlantıyı kesse de sorgular sonuna kadar çalışır. Düzeltmesi
> mekaniktir ama üç katmana birden dokunur.

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
5. **İş günü hesabı** — hafta sonu hariç. Aralık **yarı açıktır**: başlangıç dahil,
   bitiş hariç — bitiş tarihi kişinin *işe başlayacağı* gündür, izne sayılmaz
   (3'ü → 5'i = 2 gün). Sonuç sıfırsa red.
6. **Başlangıç durumu** — iki hâlde yönetici aşaması **atlanır** ve talep doğrudan
   İK onayına (`PendingHr`) düşer: (a) hastalık izni — hasta insan yönetici onayı
   bekleyemez; (b) talep sahibi zincirin tepesindeyse (`ManagerId` yok, yani GM) —
   onaylayacak üstü olmadığı için akış Admin'e muhtaç kalmasın diye. Diğerleri
   `Pending` başlar.

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
   │                    │  (hastalık izni · zincirin tepesi)
   ▼                    ▼
Pending             PendingHr
(yönetici onayı)    (İK onayı)
   │                    │
   │ yönetici onaylar   │ İK onaylar
   ├──────► PendingHr ──┴──────► Approved
   │                                 │
   └──────► Rejected                 └──► Cancelled
            (her iki aşamada da)          (sahibi, izin BAŞLAMADAN geri çeker)
```

Özel kural: talep sahibi **İK rolündeyse** yönetici onayı yeterlidir, İK aşaması
atlanır (kişi kendi talebini onaylamasın diye). Denetim izi ayrı kolonlarda
tutulur: `ManagerApprovedByUserId` / `HrApprovedByUserId` / `RejectedByUserId`
ve zaman damgaları.

**İptal / geri çekme kuralları.** Ayrı bir uç değil, aynı `DeleteLeaveRequest`
use-case'i iki farklı sonuç üretir:

| Talebin durumu | İzin başlamadı | İzne girilmiş |
|---|---|---|
| `Pending` / `PendingHr` | **İptal** — kayıt silinir | ✗ |
| `Approved` | **Geri çekme** — kayıt silinmez, `Cancelled`'a çekilir | ✗ |
| `Rejected` / `Cancelled` | ✗ (akış sonuçlanmış) | ✗ |

Onaysız talep silinir çünkü geride tutulacak bir onay izi yoktur. Onaylı talep
**silinmez**: "kim, ne zaman onayladı" bilgisi denetim için kalmalıdır. `Cancelled`
kullanılan-gün, çakışma ve takvim sorgularının statü listelerinde bulunmadığı için
günler ayrıca bir "iade" kodu yazılmadan bakiyeye döner. Başlangıç gününün sabahı
da "izne girilmiş" sayılır. Admin istisnası saklıdır (yönetimsel temizlik).

---

## 5. Yapay zekâ asistanı (`Assistant` modülü)

İK/Admin kullanıcısının doğal dille sorduğu soruyu **T-SQL'e çevirip çalıştıran** ve
sonucu Türkçe özetleyen modül ("text-to-SQL"). Kenar çubuğundaki sohbet
penceresinden (`_AssistantWidget.cshtml`) kullanılır.

### 5.1. Akış

```
Widget (JS) ──POST /Assistant/Ask──► WebUI Controller ──Refit──► API /api/assistant/ask
                                                                        │
                                          AskAssistantQueryHandler ◄─────┘
                                                │  rol + aktiflik DB'den doğrulanır
                                                ▼
                                     IAiAssistant (ClaudeAssistant)
                                                │  araç döngüsü (en çok 6 tur)
                                                │  model `run_sql` çağırır
                                                ▼
                                     SqlReadOnlyGuard ── metin denetimi
                                                ▼
                                     ISqlQueryRunner (salt okuma bağlantı)
                                                ▼
                                          SQL SERVER (db_datareader)
                                                │  en çok 200 satır, 15 sn
                                                ▼
                                     model sonucu özetler ──► Answer + ExecutedQueries
```

Soru **tarayıcıdan API'ye doğrudan gitmez**: JWT, HttpOnly cookie ticket'ının içinde
olduğu için istek önce WebUI sunucusundan geçer. Aktörün kimliği yine imzalı
claim'den okunur, gövdeden değil.

### 5.2. Katman yerleşimi — hangi parça nerede, neden

| Parça | Katman | Neden orada |
|---|---|---|
| `HrDatabaseSchema` (sistem istemi: şema, enum karşılıkları, tuzaklar) | Application | İş bilgisi — "yıllık izin hakkı kolon değil, `HireDate`'ten hesaplanır" gibi kurallar modele burada anlatılır |
| `SqlReadOnlyGuard` (SQL salt okuma mu) | Application | Güvenlik kuralı; DB'siz birim testine açık olsun diye |
| `IAiAssistant`, `ISqlQueryRunner`, `IConversationStore` | Application (arayüz) | Sağlayıcı bağımsızlığı: Application hangi modeli/SDK'yı kullandığımızı bilmez |
| `ClaudeAssistant`, `MemoryConversationStore` | Infrastructure | Anthropic SDK ve bellek önbelleği birer teknoloji detayı |
| `ReadOnlySqlQueryRunner` | Infrastructure | Sorguyu **çalıştıran** tek yer |

Model kimliği (`claude-sonnet-5`), API anahtarı ve istem önbelleği yalnızca
`ClaudeAssistant` içinde bilinir. Anahtar user-secrets'tan okunur
(`Anthropic:ApiKey`), koda ve `appsettings.json`'a yazılmaz.

> `HrDatabaseSchema`, `db/` şemasına **elle senkron tutulan** bir bağımlılıktır.
> Tablo/kolon değişirse bu dosya da güncellenmelidir; derleyici bunu yakalamaz.

### 5.3. Savunma katmanları

| Katman | Ne yapar |
|---|---|
| Rol kapısı (3 yerde) | API `[Authorize(Roles="HR,Admin")]` · handler'da DB'den rol doğrulaması · WebUI'da widget gizleme (yalnız UX) |
| `SqlReadOnlyGuard` | `SELECT`/`WITH` ile başlamalı; 21 yazma anahtar kelimesi, `xp_`/`sp_` ve çoklu ifade (`;`) reddedilir |
| **Salt okuma DB kullanıcısı** | Asistan ayrı bir bağlantı dizesi kullanır (`ReadOnlyConnection`); metin denetimi atlatılsa bile veritabanı yazmayı reddetmelidir |
| Kaynak sınırları | En çok 200 satır · 15 sn sorgu zaman aşımı · en çok 6 araç turu · soru ≤ 500 karakter |
| Konuşma yalıtımı | Geçmiş `asst:{userId}:{conversationId}` anahtarıyla tutulur — başkasının `conversationId`'sini bilmek geçmişini açmaz |
| Şeffaflık | Çalıştırılan her sorgu yanıtla birlikte kullanıcıya döner ve ekranda gösterilir |
| XSS | Model çıktısı önce kaçışlanır, sonra biçimlenir |

**Bilinçli ödün — satır bazlı yetki geçerli DEĞİLDİR.** Serbest `SELECT`
üretildiği için `EmployeeVisibility` gibi ilişki temelli kurallar bu yolda
işlemez; model tabloların tamamını okuyabilir. Modülün yalnızca **zaten her şeyi
görebilen** rollere (İK, Admin) açılmasının sebebi budur.

> **Açık bulgu:** güvenlik denetimi, `SqlReadOnlyGuard`'ın *yorumları ayıklanmış*
> metni denetlerken veritabanına *ham* metnin gitmesini kritik bir açık olarak
> işaretledi; ikinci savunma katmanı olan salt okuma DB kullanıcısı ise `db/`
> altındaki hiçbir script'te oluşturulmuyor. Ayrıntı ve çözüm:
> [`docs/guvenlik-raporu.md`](docs/guvenlik-raporu.md) — bulgu K1.

---

## 6. Kesişen endişeler (cross-cutting)

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

İş kuralı reddi için ayrı bir `Result` tipi **yoktur**: kurallar
`ValidationException` fırlatır, `GlobalExceptionHandler` bunu 400 + `BaseResponse`
zarfına çevirir. Tek kanal olması akışı basit tutuyor; bedeli, "yetkisi var mı"
gibi *soru* niteliğindeki kontrollerin de exception yakalayarak cevaplanması
(ör. detay ekranındaki `CanActNow` hesabı).

**Bilinen eksikler:**

| Eksik | Nasıl kapatılır |
|---|---|
| Merkezî loglama yok (500'ler iz bırakmıyor) | `GlobalExceptionHandler`'a `ILogger` + `LoggingBehavior` |
| Transaction / Unit of Work yok | MediatR `TransactionBehavior` |
| `CancellationToken` uçtan uca akmıyor (bkz. §3) | Controller imzaları + repository arayüzleri + `CommandDefinition` |
| Asistanın SQL denetiminde açık bulgu | [`docs/guvenlik-raporu.md`](docs/guvenlik-raporu.md) K1 |
| Integration test yok (repository SQL'leri test edilmiyor) | `WebApplicationFactory` + LocalDB/Testcontainers |

---

## 7. Yetkilendirme modeli

Rol **kaba bir kapıdır**; asıl yetki **ilişkiden** gelir.

| Katman | Ne yapar | Örnek |
|---|---|---|
| API | Rol kapısı | `[Authorize(Roles = "HR,Admin")]` |
| Application | İlişki kontrolü | Yönetici yalnızca `ManagerId` zinciriyle kendisine bağlı çalışanları görür; mentor yalnızca kendi stajyerini |
| WebUI | Görünürlük | Menü ve buton gizleme — **yalnızca UX**, güvenlik değil |

Bu yüzden bir uç hem rol etiketi taşır hem de içeriği kişiye göre süzer.
WebUI tamamen atlanıp API'ye doğrudan istek atılsa bile güvenlik aynı kalır.

Kuralın **listede gizli olan, detayda da gizli olmalıdır** biçiminde bir sonucu
vardır: bir kaydın tekil ucu (`GET /{id}`), liste ucuyla aynı kapıyı taşımak
zorundadır. Aksi hâlde liste kısıtı tek tek id denenerek atlatılır (IDOR).
Çalışan tarafında bunu `EmployeeVisibility` yapar (kural ilişkiye bağlı olduğu
için attribute yetmez); stajyer tarafında kural saf rol kuralı olduğundan aynı
`[Authorize(Roles=…)]` etiketi yeterlidir.

**Asistan bu modelin dışındadır** — serbest SQL ürettiği için ilişki temelli
süzme uygulanamaz; ayrıntı §5.3.

---

## 8. İlgili dokümanlar

| Doküman | İçerik |
|---|---|
| [`README.md`](README.md) | Kurulum ve çalıştırma |
| [`docs/veri-modeli.md`](docs/veri-modeli.md) | ER diyagramı ve veri sözlüğü |
| [`docs/teknoloji-secimleri.md`](docs/teknoloji-secimleri.md) | Teknoloji kararlarının gerekçeleri, alternatifleri, bedelleri |
| [`docs/guvenlik-raporu.md`](docs/guvenlik-raporu.md) | Güvenlik denetimi — bulgular ve çözümleri |
| [`docs/is-kurallari-raporu.md`](docs/is-kurallari-raporu.md) | İş kurallarının doğruluk denetimi |
| [`db/README.md`](db/README.md) | SQL script'leri ve şema notları |
| [`docs/TESLIMAT.md`](docs/TESLIMAT.md) | Teslimat kontrol listesi ve bilinçli sapmalar |
| [`CLAUDE.md`](CLAUDE.md) | Geliştirme kuralları |
