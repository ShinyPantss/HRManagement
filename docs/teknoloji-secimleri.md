# Teknoloji Seçimleri — Mülakat / Mentor Savunma Rehberi

Bu doküman, projedeki her teknik kararın **neden verildiğini**, **projede nasıl somutlaştığını**
ve **bedelinin ne olduğunu** anlatır. Amaç ezber değil; her satırın arkasında gerçek kod referansı
var, mentorun "göster bana" demesi durumunda dosyayı açıp gösterebilesin diye.

Format her konuda aynı: **Soru → Kısa cevap → Bu projede nasıl kullanılıyor → Alternatifi ve farkı
→ Bedeli/dezavantajı → Karşı soru gelirse.**

---

## 1. Clean Architecture / katmanlı yapı

**Soru:** "Neden 5 ayrı proje? Bu kadar katman şişkinlik değil mi, tek projede de olmaz mıydı?"

**Kısa cevap:** Katmanlar bağımlılığın YÖNÜNÜ zorunlu kılıyor: iş kuralları (Domain, Application)
hiçbir teknik detayı (veritabanı, HTTP, framework) bilmiyor; teknik detaylar iş kurallarına bağımlı.
Bu sayede veritabanını ya da UI'ı değiştirmek iş mantığını etkilemiyor — ve önemlisi, .csproj
`ProjectReference` seviyesinde derleyici bu kuralı zorluyor, sadece dokümanda yazmıyor.

**Bu projede nasıl kullanılıyor:**
`.csproj` dosyalarındaki `ProjectReference` zinciri bağımlılık yönünü fiziksel olarak kilitler:

- `HRManagement.Domain.csproj` — hiçbir `ProjectReference` yok. Domain hiçbir şeye bağımlı değil.
- `HRManagement.Application.csproj` → yalnızca `Domain`'e referans veriyor.
- `HRManagement.Infrastructure.csproj` → `Domain` + `Application`'a referans veriyor (Dapper,
  BCrypt, JWT, Anthropic paketleri burada).
- `HRManagement.API.csproj` → `Application` + `Infrastructure`'a referans veriyor; composition
  root burası (`src/HRManagement.API/Program.cs:12-15`).
- `HRManagement.WebUI.csproj` → **hiçbir iş katmanına referans YOK** — `Refit` ve `ClosedXML`
  dışında proje referansı içermiyor. WebUI, API'yle yalnızca çalışma anında HTTP üzerinden konuşuyor
  (`src/HRManagement.WebUI/Program.cs:56-108`, `AddRefitClient` çağrıları).

Bunun somut faydası: `EmployeeVisibility` gibi bir iş kuralı sınıfı
(`src/HRManagement.Application/Features/Employees/Shared/EmployeeVisibility.cs`) içinde
`SqlConnection`, `HttpContext` gibi hiçbir teknik tip **derleme zamanında dahi erişilemez** —
proje referans etmiyor, `using` bile yazılamaz.

**Alternatifi ve farkı:**

| Yaklaşım | Bağımlılık yönü | Test edilebilirlik | Yeni öğrenen için |
|---|---|---|---|
| Tek proje (katmansız) | Karışık, her şey her şeyi görür | Zor (DB'siz test imkansızlaşır) | Basit ama disiplin kaybolur |
| Bu proje (5 katman) | İçe doğru zorunlu | Application, DB olmadan test edilir | Öğrenme eğrisi var ama sınırlar net |
| Modüler monolit (feature-based) | Katman değil özellik bazlı | İyi | Bu proje boyutuna göre fazla soyutlama |

**Bedeli / dezavantajı:** 5 proje = 5 `.csproj`, daha fazla dosya gezinme, basit bir CRUD için bile
Domain→Application→Infrastructure→API şeklinde 4 dosyaya dokunmak gerekebiliyor. Küçük bir proje
için nesnel olarak fazladan tören (ceremony).

**Karşı soru gelirse:** "Bu proje küçük, tek katman yeterdi" denirse: doğru, bu proje boyutunda
zorunlu değil — ama bilinçli tercih **öğrenme** amaçlı: bağımlılık yönetimini gerçek bir mimaride
elle deneyimlemek, ileride büyüyecek bir sistemde bu disiplinin neden gerektiğini görmek için.
Ayrıca WebUI/API ayrımı gerçek bir ihtiyaçtan doğuyor: tarayıcı hiçbir zaman iş katmanına
doğrudan erişmiyor, her şey HTTP + JWT üzerinden denetimli geçiyor.

---

## 2. CQRS + MediatR

**Soru:** "MediatR ne çözüyor? Handler'ları doğrudan servis olarak da çağırabilirdin."

**Kısa cevap:** MediatR, "her use-case kendi mesajı + kendi handler'ı" kalıbını (CQRS) framework
seviyesinde zorunlu kılıyor: controller'lar handler'ı asla doğrudan bilmiyor, `ISender.Send`
üzerinden mesaj gönderiyor. Bunun asıl kazancı **pipeline behavior**: validation gibi ortak bir
kaygıyı her handler'a elle yazmak yerine tek bir ara katmana koyup otomatik çalıştırabiliyoruz.

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.Application/DependencyInjection.cs:16-22`:
```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```
`ValidationBehavior<TRequest, TResponse>` (`src/HRManagement.Application/Behaviors/ValidationBehavior.cs`)
her mesajdan **handler'dan önce** geçiyor; kayıtlı bir `IValidator<TRequest>` varsa çalıştırıyor,
hata varsa `ValidationException` fırlatıp handler'ı hiç çağırmıyor. Yani 26+ handler'ın hiçbirine
"if (!ModelState.IsValid) ..." satırı yazılmadı.

Komut/sorgu ayrımı somut örnek — `CreateLeaveRequestCommand`
(`src/HRManagement.Application/Features/LeaveRequests/Commands/CreateLeaveRequest/CreateLeaveRequestCommand.cs:15-21`):
```csharp
public sealed record CreateLeaveRequestCommand(
    int RequesterUserId,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    string? MedicalReport) : IRequest<int>;
```
Pozisyonel `sealed record` — mesaj bir değer nesnesi, değişmez. `IRequestHandler` imzası
her yerde aynı: `Handle(TRequest, CancellationToken)`. Controller ince kalıyor
(`src/HRManagement.API/Controllers/LeaveRequestsController.cs:30-38`): request'i mesaja çevirir,
`_mediator.Send(...)` çağırır, sonucu response'a çevirir. İş mantığı controller'da yok.

**Sürüm 12'de sabit tutulma sebebi:** `src/HRManagement.Application/HRManagement.Application.csproj`
içinde `MediatR` paketi **12.5.0**'a sabitlenmiş. Bu bilinçli bir karar: **MediatR 13+ sürümden
itibaren ticari lisansa geçti** (Jimmy Bogard'ın kararı). Açık kaynak/ücretsiz kullanım son sürümü
12.x'te kaldı. Proje eğitim amaçlı olduğu için ücretsiz kalan son sürümde sabitlendi; `dotnet
outdated` gibi bir araç "güncelle" dese bile bilinçli olarak yükseltilmiyor.

**Alternatifi ve farkı:**

| Yaklaşım | Ne kazandırır | Ne kaybettirir |
|---|---|---|
| MediatR (bu proje) | Tek tip mesaj/handler, otomatik pipeline (validation, ileride logging) | Ekstra dolaylılık (indirect call), "handler nerede?" aramayı gerektirir |
| Doğrudan servis enjeksiyonu (`IEmployeeService.Create(...)`) | Basit, doğrudan, IDE "go to definition" tek adımda | Her ortak kaygı (validation, loglama) elle her metoda yazılır |
| Özel marker interface (`ICommand`, `IQuery`) | Komut/sorgu ayrımı tip sisteminde daha görünür | MediatR'ın sağladığı hazır pipeline altyapısından koparsın, elle yeniden kurarsın |

**Bedeli / dezavantajı:** Bir isteğin nereye gittiğini bulmak "handler'ı ara" adımını gerektiriyor
(IDE'de `Send` çağrısından handler'a doğrudan atlama, arayüz metod çağrısı kadar keskin değil).
Küçük bir CRUD için bile Command + Handler + Validator = 3 dosya. Ayrıca 13+ sürüme geçmek isteyen
biri lisans ücreti ödemek zorunda kalır — bu proje o riske hiç girmiyor.

**Karşı soru gelirse:** "Neden 13'e geçmiyorsun, ücretsiz sürüm desteklenmeyecek mi?" —
12.x açık kaynak (Apache 2.0) sürümü olarak kalmaya devam ediyor, sadece yeni özellik almıyor.
Proje ölçeğinde ihtiyaç duyulan her şey zaten 12.x'te var; lisans riski almanın gerekçesi yok.

---

## 3. Dapper vs EF Core

**Soru:** "Neden EF Core değil, Dapper? Change tracking, migration gibi kolaylıklardan neden
vazgeçtin?"

**Kısa cevap:** Dapper, SQL'i **kendin yazmanı** zorunlu kılan ince bir mapping katmanı; EF Core
ise SQL'i senin yerine üreten bir ORM. Bu projede SQL'i elle yazmak hem performans/kontrol
kazandırıyor hem de öğrenme değeri taşıyor — bedeli, EF Core'un otomatik yaptığı her şeyi (change
tracking, migration, ilişki yükleme) elle üstlenmek.

**Bu projede nasıl kullanılıyor:**
`EmployeeRepository.GetTeamAsync` (`src/HRManagement.Infrastructure/Persistence/EmployeeRepository.cs:173-199`)
— bir yöneticinin **tüm alt zincirini** (astların astları...) tek sorguda getiren özyinelemeli CTE:
```sql
WITH Team AS (
    SELECT Id, 1 AS Depth FROM Employees WHERE ManagerId = @ManagerId
    UNION ALL
    SELECT e.Id, t.Depth + 1 FROM Employees e JOIN Team t ON e.ManagerId = t.Id
    WHERE t.Depth < 32
)
SELECT em.* FROM Employees em JOIN Team t ON t.Id = em.Id;
```
`Depth < 32` sınırı bilinçli: veri hatası yüzünden oluşacak bir döngüyü (A yöneticisi B, B
yöneticisi A) veya aşırı derinliği kesiyor. Aynı desenin tersi `IsInManagerChainAsync`
(`EmployeeRepository.cs:201-229`) — bir kişinin başka birinin yönetici zincirinde olup olmadığını
yukarı doğru CTE ile kontrol ediyor. Bu sorgu `EmployeeVisibility.EnsureCanViewAsync`
(`src/HRManagement.Application/Features/Employees/Shared/EmployeeVisibility.cs:98-100`) içinde
**yetki kontrolü** için kullanılıyor — yani performans değil, doğruluk kritik bir noktada.

`UpdateAsync` (`EmployeeRepository.cs:46-72`) elle yazılmış bir `UPDATE` — her sütun tek tek
listeleniyor, `UpdatedAt = SYSUTCDATETIME()` bilinçli olarak DB saatinden yazılıyor (istemci
saatine güvenilmiyor). `DeleteWithAccountAsync` (`EmployeeRepository.cs:83-115`) elle yönetilen bir
transaction: çalışan silinirken bağlı hesap talepleri siliniyor, login hesabı hard-delete yerine
`IsActive = 0` ile pasife alınıyor (FK bütünlüğü ve denetim izi için), sonra çalışan siliniyor;
hata olursa `transaction.Rollback()` elle çağrılıyor. EF Core'da bu, `SaveChangesAsync()` tek
çağrısına ve `DbContext`'in kendi change tracker'ına devredilirdi.

**Alternatifi ve farkı:**

| | Dapper (bu proje) | EF Core |
|---|---|---|
| SQL kontrolü | Sen yazarsın, tam görürsün | ORM üretir, üretilen sorguyu görmek için loglama açman gerekir |
| Performans | İnce mapping, ekstra soyutlama yok | Değişiklik izleme + sorgu derleme ek yük getirir |
| Migration | Yok — `db/01_schema.sql` tek doğruluk kaynağı, elle yazılır | `dotnet ef migrations add` ile otomatik üretilir |
| İlişki yükleme | Elle JOIN/CTE yazarsın (`GetTeamAsync`) | `.Include()` ile bildirimsel |
| Refactor güvenliği | Sütun adı SQL string'inde — yanlış yazarsan runtime'da patlar | LINQ ifadesi C# tipine bağlı — derleyici çoğu hatayı yakalar |
| Öğrenme değeri | SQL'i gerçekten yazıp anlıyorsun (CTE, transaction, JOIN) | SQL'in çoğu senden gizlenir |

**Bedeli / dezavantajı:** Dürüst liste — bu projede bedeli fiilen ödenen yerler:
- **Change tracking yok:** `UpdateAsync` tüm sütunları her seferinde yazıyor, "sadece değişeni
  güncelle" optimizasyonu elle yapılmadı.
- **Migration yok:** Şema değişikliği = yeni bir `db/NN_*.sql` dosyası elle yazılıp elle
  çalıştırılıyor (`db/` klasöründeki 19 dosyaya bakılırsa bu ciddi bir bakım yükü — bkz.
  `db/13_flatten_manager_chains.sql`, `db/17_fix_gm_manager_chain.sql` gibi düzeltme script'leri).
- **Refactor güvenliği düşük:** Bir sütun adı değişirse, o sütunu kullanan her SQL string'i elle
  bulunup düzeltilmeli; derleyici yardımcı olmaz.
- **Elle transaction yönetimi:** `DeleteWithAccountAsync`'teki gibi çok adımlı işlemlerde
  `BeginTransaction`/`Commit`/`Rollback` elle yazılıyor; unutulursa veri tutarsızlığı riski var.

**Karşı soru gelirse:** "EF Core da raw SQL çalıştırabilir (`FromSqlRaw`), o zaman neden hiç EF
Core kullanmadın?" — Doğru, ama o zaman iki dünyanın da karmaşıklığını taşırsın (DbContext
kurulumu + değişiklik izleme + bir de elle SQL). Bu proje bilinçli olarak "ya hep ya hiç" gitti:
SQL kontrolünü tam istiyorsan Dapper'ın kendisiyle kal, EF Core'un LINQ katmanını hiç devreye
sokma. `CLAUDE.md`'de bu kural açık: "EF Core YOK ve EKLENMEYECEK."

---

## 4. MSSQL vs MongoDB

**Soru:** "İK verisi neden ilişkisel bir veritabanında? MongoDB kullansaydın daha esnek olmaz
mıydı?"

**Kısa cevap:** İK verisi doğası gereği **ilişkisel**: çalışan → departman → birim → yönetici →
izin talebi zinciri, hepsi birbirine foreign key ile bağlı ve sorgular sık sık bu zinciri JOIN'le
geziyor. MongoDB doküman modeli, aynı veriyi ya gömülü (denormalize, tutarsızlık riski) ya da elle
`$lookup` ile birleştirilen ayrı koleksiyonlar (JOIN'i uygulamaya taşımak) olarak tutmak zorunda
kalırdı — ilişkisel modelin zaten çözdüğü bir sorunu yeniden icat etmiş olurduk.

**Bu projede nasıl kullanılıyor:**
`db/01_schema.sql:83-84` — foreign key'ler şemada **veritabanı seviyesinde** garanti ediliyor:
```sql
CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
CONSTRAINT FK_Employees_Users       FOREIGN KEY (UserId)       REFERENCES dbo.Users (Id)
```
Yorum satırı bunu açıkça söylüyor: "Benzersizliği uygulama da kontrol ediyor, ama asıl garantiyi
kısıt verir: uygulama dışı kayıtlar ve eşzamanlı istekler ancak böyle engellenir." MongoDB'de bu
garanti yok — foreign key kavramı yok, referans bütünlüğünü tamamen uygulama koduna güvenmen
gerekir.

En güçlü kanıt, zincir sorguları: `GetTeamAsync` ve `IsInManagerChainAsync`
(`src/HRManagement.Infrastructure/Persistence/EmployeeRepository.cs:173-229`) organizasyon
şemasında **değişken derinlikte** yukarı/aşağı gezinme yapıyor — özyinelemeli CTE (`WITH ... AS
(... UNION ALL ...)`) ile tek sorguda, veritabanı motorunun optimize ettiği şekilde çözülüyor.
MongoDB'de karşılığı ya:
- `$graphLookup` aggregation operatörü (var, ama SQL CTE kadar doğal değil, performans
  karakteristiği farklı ve hata ayıklaması zor), ya da
- Uygulama tarafında elle döngü: "yöneticiyi çek → ID'sini al → tekrar sorgula → tekrarla" —
  N+1 sorgu problemi, ve döngü/derinlik koruması (`Depth < 32`) SQL'de tek satırken uygulama
  kodunda elle yazılan bir while döngüsüne dönüşürdü.

`EmployeeVisibility.GetVisibleAsync`
(`src/HRManagement.Application/Features/Employees/Shared/EmployeeVisibility.cs`) bu CTE'lerin
sonucuna güveniyor — yetki kararı ("bu kişi bu çalışanı görebilir mi") doğrudan ilişkisel
sorgunun doğruluğuna dayanıyor.

**Transaction/ACID ihtiyacı:** `DeleteWithAccountAsync`
(`EmployeeRepository.cs:83-115`) üç adımı (hesap taleplerini sil → login hesabını pasife al →
çalışanı sil) tek transaction'da yapıyor; ortada patlarsa hepsi geri alınıyor. MSSQL'de bu
`BeginTransaction`/`Commit`/`Rollback` ile doğal. MongoDB'de çoklu-doküman transaction 4.0'dan
beri var ama ek karmaşıklık ve performans maliyeti getiriyor — MongoDB'nin güçlü olduğu senaryo
zaten "tek doküman atomik yeter" olan senaryolardır, bu proje onun tam tersi.

**Alternatifi ve farkı:**

| | MSSQL (bu proje) | MongoDB |
|---|---|---|
| Veri şekli | Sabit şema, tablo+satır, FK ile bağlı | Esnek şema, doküman+koleksiyon |
| İlişki gezinme (yönetici zinciri) | Tek CTE sorgusu | `$graphLookup` ya da uygulamada döngü |
| Referans bütünlüğü | FK kısıtı veritabanı seviyesinde garanti | Uygulama sorumluluğu |
| Çoklu-kayıt transaction | Doğal (`BEGIN TRAN`) | Var ama ek karmaşıklık, performans maliyeti |
| Şema değişikliği | Migration/ALTER gerekir (yavaş, planlı) | Doküman başına farklı şekil, anında esner |
| Yatay ölçekleme | Zor (sharding karmaşık) | Doğal güçlü yanı |

**Bedeli / dezavantajı:** İlişkisel şema **katı**: `db/` klasöründeki 19 dosyanın çoğu (örn.
`db/08_drop_position.sql`, `db/14_employee_gender.sql`) şema evrimi için elle yazılan `ALTER
TABLE` script'leri — MongoDB'de yeni bir alan eklemek için migration script yazmana gerek kalmazdı,
doküman doğrudan yeni alanla kaydedilirdi.

**MongoDB'nin gerçekten daha iyi olacağı senaryolar (dürüstçe):**
- Şeması sık ve öngörülemez şekilde değişen veri (ör. her kullanıcının farklı özel alanlar
  eklediği bir form/anket sistemi).
- Doküman başına bağımsız, ilişkisel sorgu gerektirmeyen büyük hacimli veri (log kaydı, event
  stream, sensör verisi).
- Yatay ölçekleme (sharding) gerçek bir ihtiyaçsa — İK sistemi hiçbir zaman bu ölçeğe ulaşmaz.
- Doküman içinde doğal olarak iç içe geçen, JOIN gerektirmeyen veri (ör. bir ürünün varyantları).

Bu proje için hiçbiri geçerli değil: veri hacmi küçük, şema stabil, ve her sorgu tam olarak
ilişkisel modelin güçlü olduğu "birden çok varlığı ilişkiye göre birleştir" işini yapıyor.

**Karşı soru gelirse:** "MongoDB de JOIN yapabiliyor (`$lookup`), o zaman fark ne?" — `$lookup` bir
aggregation adımıdır, SQL JOIN'i gibi optimize edici tarafından native planlanmaz; çok adımlı,
çok tablolu sorgularda (ör. `GetTeamAsync`'in yaptığı özyinelemeli gezinme) karmaşıklaşır ve
performans SQL motorunun onlarca yıllık JOIN optimizasyonuna genelde yetişemez. Ayrıca referans
bütünlüğü garantisi (FK) hâlâ yok — `$lookup` başarısız bir referansı sessizce boş dizi olarak
döner, SQL'de ise FK ihlali baştan INSERT'i reddeder.

---

## 5. Çift kimlik modeli: Cookie + JWT

**Soru:** "Neden tek bir kimlik doğrulama yöntemi değil de ikisi birden? Karmaşıklaştırmıyor mu?"

**Kısa cevap:** İki farklı güven sınırı var: tarayıcı↔WebUI ve WebUI↔API. Tarayıcıya JWT
verilseydi, JavaScript'in (ve dolayısıyla bir XSS açığının) token'ı okuyup çalabileceği bir yüzey
açılırdı. Cookie `HttpOnly` olduğu için JS token'a hiç erişemiyor; token yalnızca sunucu
tarafında, cookie ticket'ının içinde yaşıyor ve WebUI'nin API'ye yaptığı sunucu-sunucu isteklerine
ekleniyor.

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.WebUI/Program.cs:17-31`:
```csharp
// Tarayıcı ↔ WebUI kimliği: COOKIE. (WebUI ↔ API kimliği ise JWT — BearerTokenHandler'a bak.)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;   // JS'in okuyamaması kritik (XSS koruması)
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });
```
JWT token, login sonrası cookie ticket'ının **içine** gömülüyor (`AuthenticationProperties` /
`GetTokenAsync("access_token")` mekanizmasıyla). Her API isteğinde `BearerTokenHandler`
(`src/HRManagement.WebUI/Services/BearerTokenHandler.cs:24-40`) bu token'ı cookie'den okuyup
`Authorization: Bearer ...` başlığına ekliyor:
```csharp
var token = await httpContext.GetTokenAsync("access_token");
if (!string.IsNullOrWhiteSpace(token))
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
```
Bu handler, `AddRefitClient<IEmployeeApi>().AddHttpMessageHandler<BearerTokenHandler>()`
(`Program.cs:64-66`) ile Refit istemcisinin isteğine otomatik takılıyor — controller'ların token
taşımak için tek satır kod yazması gerekmiyor.

API tarafında JWT **doğrulaması** (`src/HRManagement.API/DependencyInjection.cs:65-113`):
imza (`ValidateIssuerSigningKey`), issuer/audience ve süre (`ValidateLifetime`, `ClockSkew =
TimeSpan.Zero`) kontrol ediliyor. Token **üretimi** ayrı bir sınıfta:
`src/HRManagement.Infrastructure/Security/JwtTokenGenerator.cs` — Infrastructure'da, çünkü "nasıl
imzalanır" teknik bir detay.

**Alternatifi ve farkı:**

| Yaklaşım | Güvenlik | Karmaşıklık |
|---|---|---|
| Bu proje (Cookie + JWT, iki katman) | Token JS'e hiç sızmaz | İki mekanizma yönetilir |
| Tek JWT, tarayıcıya localStorage'da | XSS ile token doğrudan çalınabilir | Basit, tek mekanizma |
| Tek cookie, WebUI kendi session'ını API'ye de geçirir | API'nin cookie doğrulaması gerekir, WebUI/API arası sıkı bağ oluşur | Basit ama API'yi WebUI'ye bağımlı kılar |

**Bedeli / dezavantajı:** İki ayrı süre yönetimi var (cookie `ExpireTimeSpan` ile JWT `expires`
elle senkron tutuluyor — `Program.cs:25` yorumunda "API token'ıyla aynı ömür" notu var, bu elle
uyulması gereken bir kural, derleyici zorlamıyor). `SlidingExpiration = false` bilinçli: token
yenilenmediği için cookie'nin süresi de uzamamalı, yoksa cookie geçerli görünürken içindeki JWT
süresi dolmuş olurdu ve API sessizce 401 dönerdi.

**Karşı soru gelirse:** "Neden API'yi de cookie ile korumadın, tek mekanizma olurdu?" — API'nin
tek istemcisi WebUI değil olabilir gelecekte (mobil uygulama, başka bir servis); JWT
stateless ve taşınabilir, cookie tarayıcıya özgü bir mekanizma. Ayrıca CLAUDE.md kuralı gereği
CORS hiç eklenmiyor — WebUI→API çağrıları sunucudan yapılıyor, tarayıcıdan değil; bu da JWT'nin
tarayıcıya hiç maruz kalmamasını mimari olarak garantiliyor.

---

## 6. Refit

**Soru:** "Neden elle `HttpClient` + JSON kodu yazmadın, Refit'e ihtiyaç neydi?"

**Kısa cevap:** Refit, bir arayüz + attribute tanımından çalışma anında gerçek bir HTTP istemcisi
üretiyor. Elle yazılan `HttpClient.PostAsync(url, JsonContent.Create(...))` + manuel
deserialize kalıbının tekrarını (ve unutulan bir `Content-Type` header'ı gibi hataları) ortadan
kaldırıyor.

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.WebUI/Services/IEmployeeApi.cs:12-38`:
```csharp
public interface IEmployeeApi
{
    [Get("/api/employees")]
    Task<BaseResponse<List<EmployeeResponse>>> GetAllAsync();

    [Post("/api/employees/{id}/notes")]
    Task<BaseResponse<int?>> AddNoteAsync(int id, [Body] AddEmployeeNoteRequest request);
}
```
Burada tek satır elle HTTP kodu yok — Refit, `{id}` yol parametresini metot imzasından, gövdeyi
`[Body]` ile işaretli parametreden, serialize/deserialize'ı dönüş tipinden (`BaseResponse<T>`)
kendisi çözüyor. Kayıt `src/HRManagement.WebUI/Program.cs:56-108`'de her API grubu için
`AddRefitClient<IXxxApi>(refitSettings).ConfigureHttpClient(...)` şeklinde; veri istemcilerine
`BearerTokenHandler` zincirleniyor (bkz. bölüm 5).

**`ExceptionFactory`'nin kapatılması** (`Program.cs:50-53`):
```csharp
var refitSettings = new RefitSettings
{
    ExceptionFactory = _ => Task.FromResult<Exception?>(null)
};
```
Varsayılan davranışta Refit, HTTP durum kodu 400/401/500 gibi başarısız olduğunda
`ApiException` fırlatır. Bu projede API her hata durumunda da **aynı `BaseResponse` zarfını**
gövdede döndürdüğü için (bkz. bölüm 8), exception fırlatmak yerine yanıtı olduğu gibi
deserialize edip `IsSuccess=false` olarak okumak daha tutarlı — WebUI tarafında her çağrının
`try/catch` yerine `if (!response.IsSuccess)` ile ele alınmasını sağlıyor.

**Alternatifi ve farkı:**

| Yaklaşım | Kod miktarı | Hata riski |
|---|---|---|
| Refit (bu proje) | Arayüz + attribute, implementasyon üretilir | Düşük — URL/method/body eşleşmesi derleme zamanı arayüzde görünür |
| Elle `HttpClient` | Her çağrı için `PostAsync`/`GetAsync` + manuel `JsonSerializer` | Yüksek — URL string'i, header, serialize adımı unutulabilir |
| `HttpClientFactory` + generic wrapper | Orta | Refit'in tip güvenliğinden daha az |

**Bedeli / dezavantajı:** Refit'in ürettiği kod çalışma anında (runtime, dynamic proxy /
source generator) oluşuyor — hata ayıklarken "gerçek HTTP isteği ne gönderiyor?" sorusuna
Refit'in kendi loglama/debugging araçlarına bakman gerekebiliyor, elle yazılmış kod kadar
şeffaf değil. Ayrıca yeni bir uç eklerken hem API tarafında controller action'ı hem WebUI
tarafında Refit arayüz metodunu **elle senkron** tutman gerekiyor (paylaşılan Contracts yok,
bkz. bölüm 9).

**Karşı soru gelirse:** "Peki neden `ExceptionFactory`'yi tamamen kapatmak yerine sadece
belirli durum kodlarını yakalamadın?" — Çünkü API'nin sözleşmesi zaten net: her yanıt (başarı
ya da hata) `BaseResponse` zarfı taşıyor. Exception mekanizmasını kullanmak, zaten gövdede var
olan bilgiyi (`IsSuccess`, `Message`) tekrar exception'a sarıp açmak anlamına gelirdi —
gereksiz bir dolaylılık.

---

## 7. FluentValidation

**Soru:** "DataAnnotations (`[Required]`, `[MaxLength]`) yeterli değil miydi, neden ayrı bir
kütüphane?"

**Kısa cevap:** DataAnnotations kuralları modelin **üzerine** attribute olarak yazılır ve
karmaşık/koşullu kurallarda (bir alanın değeri diğerine bağlıysa) hızla yetersiz kalır.
FluentValidation kuralları ayrı bir sınıfta, akıcı (fluent) bir API ile, tam C# ifade gücüyle
yazılır — ve MediatR pipeline'ına otomatik takılır.

**Bu projede nasıl kullanılıyor:**
`CreateLeaveRequestCommandValidator`
(`src/HRManagement.Application/Features/LeaveRequests/Commands/CreateLeaveRequest/CreateLeaveRequestCommandValidator.cs:14-41`)
koşullu kural örneği:
```csharp
RuleFor(command => command.MedicalReport)
    .NotEmpty().When(command => command.Type == LeaveType.Sick)
    .WithMessage("Hastalık izni için rapor bilgisi zorunludur.");
```
`.When(...)` — "yalnızca izin türü Sick ise rapor zorunlu" gibi bir kural DataAnnotations'ta
`[Required]` ile ifade edilemez; ya elle `IValidatableObject` implementasyonu gerekir ya da
handler içine if yazmak gerekirdi (ki CLAUDE.md kuralı bunu yasaklıyor: "Handler içine
input-validation if'i yazılmaz").

Otomatik çalışma: `services.AddValidatorsFromAssembly(assembly)`
(`src/HRManagement.Application/DependencyInjection.cs:25`) — `AbstractValidator<T>`'dan türeyen
her sınıf otomatik kaydedilir; yeni bir validator eklemek için DI kaydına dokunmaya gerek yok.
`ValidationBehavior<TRequest,TResponse>`
(`src/HRManagement.Application/Behaviors/ValidationBehavior.cs:27-44`) o mesaj tipine kayıtlı
validator'ları bulup **handler'dan önce** çalıştırıyor; hata varsa handler hiç tetiklenmiyor.

**Alternatifi ve farkı:**

| | FluentValidation (bu proje) | DataAnnotations |
|---|---|---|
| Koşullu kural | `.When(...)` ile doğal | `IValidatableObject.Validate()` elle yazılır |
| Kural yeri | Ayrı sınıf, model temiz kalır | Attribute modelin üzerinde, model şişer |
| Pipeline entegrasyonu | `ValidationBehavior` ile otomatik, tek yerde | `[ApiController]` model binding'e gömülü, handler'a hiç sızmaz |
| Test edilebilirlik | Validator tek başına, bağımsız test edilir | Model + attribute birlikte test edilir |

**Bedeli / dezavantajı:** Her command/query için ayrı bir `XxxValidator.cs` dosyası — basit bir
sorgu bile (validator gerekmese de) "validator yok mu, bilerek mi atlandı?" sorusunu akla
getirebilir. Ayrıca iki farklı doğrulama kaynağı var projede: FluentValidation (input) ve
handler içindeki iş kuralı reddi (`DataAnnotations.ValidationException` fırlatan, ör.
`CreateLeaveRequestCommandHandler.cs:38-121`) — ikisinin farkını (input vs iş kuralı) bilmek
gerekiyor, karıştırılırsa yanlış katmana kural yazılır.

**Karşı soru gelirse:** "İki farklı `ValidationException` tipi (FluentValidation'ınki ve
DataAnnotations'ınki) kafa karıştırıcı değil mi?" — Bilinçli bir ayrım: FluentValidation'ın
`ValidationException`'ı **input** hatası ("tarih formatı geçersiz"), DataAnnotations'ın
`ValidationException`'ı (`System.ComponentModel.DataAnnotations`) **iş kuralı** reddi ("izin
hakkı yetersiz"). `GlobalExceptionHandler`
(`src/HRManagement.API/Middleware/GlobalExceptionHandler.cs:21-31`) ikisini de yakalayıp aynı
`BaseResponse` zarfına, aynı 400 koduna çeviriyor — istemci için fark görünmez, kod okuyan için
"bu hata nereden geldi" sorusuna netlik katıyor.

---

## 8. BaseResponse zarfı vs ProblemDetails

**Soru:** "ASP.NET Core'un standart `ProblemDetails`'i varken neden özel bir `BaseResponse<T>`
zarfı?"

**Kısa cevap:** `ProblemDetails` ve başarılı yanıtlar (`200 OK` + veri) **farklı şekillerdedir** —
istemci "başarılı mıydı?" sorusuna cevap almak için önce durum koduna, sonra gövde şekline
bakmak zorunda kalır. `BaseResponse<T>` tek bir şekil sunuyor: her yanıt `{ IsSuccess, Message,
Data }`; istemci (Refit) her zaman aynı tipi deserialize ediyor.

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.API/Models/BaseResponse.cs:7-18`:
```csharp
public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static BaseResponse<T> Success(T data, string? message = null) => ...
    public static BaseResponse<T> Fail(string message) => ...
}
```
Zarfın **deliksiz** (her senaryoda dolu) olmasını üç ayrı mekanizma birlikte garanti ediyor —
CLAUDE.md bunu açıkça üç madde olarak listeliyor ve "üçü de sökülmemeli" diyor:

1. **`GlobalExceptionHandler`** (`src/HRManagement.API/Middleware/GlobalExceptionHandler.cs`) —
   işlenmemiş her exception'ı yakalayıp `BaseResponse<object>.Fail(...)`'a çeviriyor.
2. **`ApiBehaviorOptions.InvalidModelStateResponseFactory`**
   (`src/HRManagement.API/DependencyInjection.cs:46-55`) — `[ApiController]`'ın model binding
   hatalarını (bozuk JSON) varsayılan olarak `ProblemDetails`'e çevirme davranışını eziyor;
   handler'a hiç uğramadan (exception yok, MVC kısa devre yapıyor) yine `BaseResponse` dönmesini
   sağlıyor.
3. **`UseBaseResponseStatusCodes`**
   (`src/HRManagement.API/Middleware/StatusCodeResponseExtensions.cs:19-36`) — 401/403/404 gibi
   ASP.NET Core'un **gövdesiz** döndüğü durumları (0 bayt) yakalayıp `BaseResponse` gövdesi
   ekliyor. Yorum satırı sebebi açıkça anlatıyor: "İstemcimiz (Refit) her yanıtı BaseResponse
   olarak okumaya çalıştığı için boş gövde o sözleşmeyi bozar."

`AddProblemDetails()` **bilinçli olarak eklenmiyor** — `DependencyInjection.cs:26-31` yorumu:
eklenirse framework'ün "son çare bir exception handler var mı?" başlangıç kontrolünü geçer ama
projeye `ProblemDetails` şeklini sızdırırdı. Onun yerine `ExceptionHandlerOptions.ExceptionHandler`
elle `BaseResponse` yazan bir son çare olarak tanımlanıyor (`DependencyInjection.cs:32-40`).

**Alternatifi ve farkı:**

| | BaseResponse (bu proje) | ProblemDetails (RFC 7807, ASP.NET Core standardı) |
|---|---|---|
| Şekil | Her zaman aynı: `{IsSuccess, Message, Data}` | Hata için `{type, title, status, detail}`, başarı için ayrı şekil (genelde çıplak veri) |
| İstemci tarafı | Tek tip deserialize, `if(!IsSuccess)` | Durum koduna göre farklı tip parse etmen gerekir |
| Framework desteği | Elle kurulan 3 mekanizma | Yerleşik, standart, diğer araçlarla (Swagger, API Gateway) uyumlu |
| Ekosistem tanınırlığı | Proje-özel | Endüstri standardı, dışarıdan gelen biri tanır |

**Bedeli / dezavantajı:** Standart dışı bir sözleşme — projeye yeni katılan ya da dışarıdan
entegre olan biri `ProblemDetails`'i bilir, `BaseResponse`'u önce öğrenmesi gerekir. Ayrıca üç
mekanizmanın **birlikte** çalışması gerekiyor; biri unutulursa (ör. yeni bir middleware eklenip
`UseBaseResponseStatusCodes` çağrısı yanlış sıraya konursa) zarf sessizce delinir ve WebUI
tarafında `IsSuccess`/`Message` okunamaz hâle gelir — CLAUDE.md'nin "üçü de sökülmemeli" uyarısı
tam olarak bu kırılganlığa işaret ediyor.

**Karşı soru gelirse:** "Neden `ProblemDetails`'i genişletip kendi alanlarını eklemedin, sıfırdan
mı yazdın?" — `ProblemDetails`'i genişletmek bile başarı/hata için iki farklı şekil sorununu
çözmüyor: `ProblemDetails` sadece hata durumları için tasarlanmış bir standart, başarılı yanıtın
şeklini kapsamıyor. Tek zarfın hem başarı hem hata için aynı olması gereksinimi, `BaseResponse`'u
sıfırdan tanımlamayı daha basit kıldı.

---

## 9. Paylaşılan Contracts projesi olmaması

**Soru:** "API ve WebUI aynı JSON'u konuşuyor, neden ortak bir Contracts/Shared projesi yok? Kod
tekrarı değil mi?"

**Kısa cevap:** Bilinçli bir mentor kararı (CLAUDE.md, 2026-07-20): her host kendi modelini
tutuyor — API kendi `API/Models`'ini, WebUI kendi `WebUI/Models/Api`'sini. Kazanım gevşek
bağlılık (WebUI, API'nin iç tiplerine derleme zamanında bağımlı değil); bedeli, JSON şeklini iki
tarafta elle senkron tutmak.

**Bu projede nasıl kullanılıyor:**
Aynı kavram (`EmployeeResponse`) **iki ayrı sınıf** olarak, iki ayrı projede yaşıyor:

`src/HRManagement.API/Models/Employees/EmployeeModels.cs:116` (API tarafı, `sealed class`,
kurucu ile immutable):
```csharp
public sealed class EmployeeResponse
{
    public EmployeeResponse(int id, string firstName, string lastName, ... ) { ... }
    public int Id { get; }
    ...
}
```

`src/HRManagement.WebUI/Models/Api/Employees/EmployeeApiModels.cs:12-30` (WebUI tarafı, `class`,
`get; set;`):
```csharp
// API'nin Models/Employees tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)
public class EmployeeResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    ...
}
```
Dosyanın kendi yorumu bedeli açıkça itiraf ediyor: senkronu tutmak **insan sorumluluğu**,
derleyici bunu doğrulamıyor. API tarafında bir alan eklenip WebUI tarafı güncellenmezse, Refit o
alanı sessizce `default` değerle deserialize eder (JSON'da eksik alan hata vermez) — hata anında
değil, kullanım anında (ekranda boş görünen bir alan olarak) fark edilir.

**Alternatifi ve farkı:**

| Yaklaşım | Bağımlılık | Senkron | Hata anı |
|---|---|---|---|
| Bu proje (ayrı modeller) | WebUI, API'nin projesine hiç referans vermiyor | Elle, iki dosya güncellenir | Runtime (sessiz varsayılan değer) |
| Paylaşılan Contracts projesi | Her iki host da Contracts'a referans verir | Derleyici zorlar — tip değişince her iki taraf da derleme hatası alır | Derleme zamanı |
| OpenAPI/Swagger'dan kod üretimi | Kaynak API'nin OpenAPI şeması | Yarı otomatik — üretim adımı çalıştırılmalı | Üretim adımı atlanırsa runtime |

**Bedeli / dezavantajı:** Tam olarak sorudaki gibi — kod tekrarı var, ve senkronizasyon
**derleyici tarafından değil insan tarafından** garanti ediliyor. Bu projede 9+ Refit arayüzü
(`IEmployeeApi`, `IDepartmentApi`, `IInternApi`, ...) için karşılığında 9+ paralel model seti
var; her yeni alan eklemede iki dosyaya dokunmak unutulabilir bir adım.

**Karşı soru gelirse:** "Peki neden bir Contracts projesi eklenmedi, bariz kod tekrarı azaltırdı?"
— Gevşek bağlılık burada bilinçli tercih: Contracts projesi eklenseydi, WebUI dolaylı da olsa
API'nin veri şekline **derleme zamanı bağımlı** hâle gelirdi — CLAUDE.md'nin "WebUI hiçbir iş
katmanına referans vermez" kuralının ruhuna aykırı düşerdi (Contracts teknik olarak iş katmanı
değil ama API'nin implementasyon detaylarına sızma riski taşır). Ayrıca gerçek dünyada WebUI ve
API ayrı deploy edilebilecek servisler olarak düşünülürse, ikisinin aynı derleme birimini
paylaşması "aslında tek bir uygulama" gibi davranmak anlamına gelir — bu projenin host'lar
arası HTTP sınırını ciddiye alma ilkesiyle çelişir.

---

## 10. Server-rendered MVC (Razor) vs SPA (React/Angular)

**Soru:** "Neden React/Angular gibi bir SPA değil de klasik MVC? Modern değil mi SPA?"

**Kısa cevap:** Bu proje bir İK iç aracı — kullanıcı etkileşimi form doldurma, liste görüntüleme,
onay/red gibi **sayfa bazlı** işlemlerden oluşuyor; SPA'nın asıl kazandırdığı şey (zengin,
sayfa yenilenmeden akan istemci-taraflı durum) burada ihtiyaç değil. MVC + Razor, cookie tabanlı
kimlik doğrulamayla doğal olarak örtüşüyor ve JWT'nin tarayıcıya hiç sızmamasını kolaylaştırıyor
(bkz. bölüm 5).

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.WebUI/Program.cs:12-15` — MVC + global yetkilendirme filtresi:
```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
```
API'deki `FallbackPolicy` ile aynı ilke: her sayfa varsayılan olarak giriş ister. Bir SPA'da bu
kontrolün önemli kısmı istemci tarafına (route guard) kayardı ve gerçek yetki her zaman API'de
kalırdı — burada ise MVC controller seviyesinde de bir katman var, `[AllowAnonymous]` bilinçli
istisna.

Kimlik doğrulama tamamen cookie tabanlı (`Program.cs:17-31`) — sunucu tarafında session/ticket
yönetimi, tarayıcıya token hiç verilmiyor. Bir SPA olsaydı (API'ye doğrudan tarayıcıdan istek),
JWT'yi bir şekilde tarayıcıda tutmak (localStorage veya cookie) gerekirdi ve CORS açmak
zorunlu olurdu — CLAUDE.md'nin "CORS eklenmez" kuralı SPA mimarisiyle **uyumsuz** olurdu.

Türkçe kültür/format desteği (`Program.cs:112-120`) sunucu tarafında merkezi olarak
ayarlanıyor — `dd.MM.yyyy` tarih formatı, model binding seviyesinde. SPA'da bu her bileşende
istemci tarafında elle tekrarlanan bir kaygı olurdu.

**Alternatifi ve farkı:**

| | MVC/Razor (bu proje) | SPA (React/Angular + API) |
|---|---|---|
| Kimlik doğrulama | Cookie, sunucu tarafı, JWT tarayıcıya hiç sızmaz | Token tarayıcıda tutulmak zorunda (XSS yüzeyi) |
| CORS | Gerekmiyor (sunucu-sunucu istek) | Zorunlu (tarayıcı doğrudan API'ye istek atar) |
| İlk yükleme | Sunucu HTML'i render eder, hızlı ilk boyama | JS bundle indirilip çalışana kadar boş ekran |
| Etkileşim zenginliği | Sayfa bazlı, form odaklı | Zengin, sayfa yenilenmeden akan durum |
| Geliştirme hızı (bu ekip için) | Tek dil (C#), tek proje tipi | İki ayrı stack (backend + frontend), daha fazla araç |

**Bedeli / dezavantajı:** Zengin, anlık etkileşim gerektiren ekranlarda (ör. sürükle-bırak
organizasyon şeması, canlı filtreleme) MVC/Razor daha fazla sayfa yenilemesi ya da elle yazılan
JS/AJAX gerektirir. SPA ekosisteminin sunduğu component tekrar kullanımı, state management
kütüphaneleri gibi araçlardan yararlanılmıyor.

**Karşı soru gelirse:** "Peki SPA + JWT + CORS ile de XSS'e karşı önlem alınabilirdi (ör.
`httpOnly` cookie'de token tutmak), neden hiç değerlendirilmedi?" — O çözüm de mümkün ama pratikte
bu projenin ihtiyacı yok: SPA'nın gerçek kazancı olan zengin istemci-taraflı etkileşim burada
talep edilmiyor, buna karşılık MVC daha az hareketli parça (tek stack, CORS yok, token hiç
tarayıcıya değmiyor) ile aynı güvenlik hedefine daha basit ulaşıyor.

---

## 11. BCrypt

**Soru:** "Parolaları neden SHA256 gibi basit bir hash ile saklamadın, BCrypt'e ihtiyaç neydi?"

**Kısa cevap:** SHA256 **hızlı** olacak şekilde tasarlanmış bir hash — bu, parola saklamak için
tam ters yönde bir özellik: saldırgan çalınan hash'leri saniyede milyarlarca deneyerek brute-force
edebilir. BCrypt bilinçli olarak **yavaş** (ayarlanabilir "work factor") ve **salt'ı otomatik**
üretip hash'in içine gömüyor — aynı parolayı giren iki kullanıcı bile farklı hash alıyor.

**Bu projede nasıl kullanılıyor:**
`src/HRManagement.Infrastructure/Security/PasswordHasher.cs:7-15`:
```csharp
public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
```
`HashPassword` her çağrıda **rastgele bir salt** üretip hash'in başına ekliyor (BCrypt çıktısı
`$2a$11$<salt><hash>` formatında, tek bir string, salt ayrı saklanmasına gerek yok). `Verify` bu
salt'ı hash'ten okuyup aynı hesaplamayı tekrarlıyor — parolayı asla çözmüyor (BCrypt tek yönlü),
yalnızca "bu parola bu hash'i üretir mi?" diye kıyaslıyor.

`db/01_schema.sql:39,47` — `Users.PasswordHash` sütunu bilinçli olarak `nvarchar(255)`:
yorum satırı "BCrypt hash'i 60 karakterdir, bolca yer var... Bu sütun dar olursa hash sessizce
kırpılır ve giriş hiç çalışmaz" diyor — somut bir tasarım detayı, BCrypt'in çıktı uzunluğunu
(sabit ~60 karakter) bilerek şema tasarlandığını gösteriyor.

**Alternatifi ve farkı:**

| | BCrypt (bu proje) | SHA256 / MD5 (çıplak) |
|---|---|---|
| Hız | Kasıtlı yavaş (work factor ile ayarlanabilir) | Çok hızlı — brute-force'a açık |
| Salt | Otomatik üretilir, hash içine gömülür | Elle eklenmeli, elle saklanmalı |
| Rainbow table direnci | Salt sayesinde yüksek | Salt yoksa rainbow table ile anında kırılır |
| Amaç | Parola hash'lemek için tasarlandı | Genel amaçlı bütünlük/checksum için tasarlandı |

**Bedeli / dezavantajı:** BCrypt, SHA256'ya göre CPU açısından daha pahalı — yoğun trafikte
(saniyede binlerce login denemesi) bu maliyet fark yaratabilir. Bu proje ölçeğinde (İK iç aracı,
sınırlı kullanıcı sayısı) hiç sorun değil. Ayrıca BCrypt'in çıktı uzunluğu 72 baytlık bir parola
sınırı taşır (BCrypt algoritmasının kendi kısıtı) — bu projede parola uzunluğu bu sınırın çok
altında kaldığı için etkisi yok.

**Karşı soru gelirse:** "Neden Argon2 değil, o daha yeni ve daha güvenli değil mi?" — Doğru,
Argon2 (özellikle Argon2id) günümüzde OWASP'ın önerdiği ilk seçim ve GPU/ASIC saldırılarına karşı
BCrypt'ten daha dirençli. BCrypt yine de endüstride uzun süredir kanıtlanmış, .NET ekosisteminde
(`BCrypt.Net-Next`) hazır ve iyi test edilmiş bir kütüphane olarak mevcut; bu projenin tehdit
modeli (iç İK aracı, internet geneline açık olmayan) için BCrypt yeterli güvenlik marjı
sağlıyor. Üretim ölçeğinde halka açık bir sistem olsaydı Argon2id'yi ciddi şekilde değerlendirmek
doğru olurdu.

---

## 12. Yapay zekâ asistanı mimarisi

**Soru:** "Yapay zekâ asistanı veritabanına serbest SQL çalıştırıyor — bu güvenli mi? Nasıl
kontrol ediyorsun?"

**Kısa cevap:** İki bağımsız güvenlik katmanı var: (1) Application katmanında SQL metnini
`SELECT` dışını reddeden bir metin denetimi, (2) Infrastructure'da sorgunun yalnızca **yazma
yetkisi olmayan** bir veritabanı kullanıcısıyla çalıştırılması. Birincisi atlatılabilir bir
regex kontrolüdür; ikincisi veritabanı motorunun kendisi tarafından uygulanan, atlatılamaz bir
garanti. İkisi birlikte "tek noktada arızaya" karşı savunma derinliği sağlıyor.

**Bu projede nasıl kullanılıyor:**

**Katman 1 — metin denetimi**, `SqlReadOnlyGuard`
(`src/HRManagement.Application/Features/Assistant/Shared/SqlReadOnlyGuard.cs:16-101`):
sadece `SELECT`/`WITH` ile başlayan, `INSERT/UPDATE/DELETE/DROP/EXEC/...` gibi yasaklı
anahtar kelime içermeyen, tek ifadelik (`;` ile zincirlenmemiş) sorgulara izin veriyor. Yorumlar
denetimi atlatmak için kullanılabildiğinden (`SEL/**/ECT`) önce yorumlar **boşlukla** temizleniyor
(silmek değil — silinseydi `SEL/**/ECT` → `SELECT`e dönüşüp denetimi geçerdi). Dosyanın kendi
yorumu dürüst: "Bu tek başına YETERLİ DEĞİLDİR."

**Katman 2 — veritabanı yetkisi**, `DbConnectionFactory.CreateReadOnlyConnection`
(`src/HRManagement.Infrastructure/Persistence/DbConnectionFactory.cs:38-48`): ayrı bir
`ReadOnlyConnection` bağlantı dizesi, ayrı bir SQL kullanıcısı (yalnızca `db_datareader` rolü).
Yorum satırı: "koddaki metin denetimi (SqlReadOnlyGuard) atlatılsa bile veritabanı yazma
girişimini reddeder. Güvenliğin tek bir regex'e bağlı kalmaması için." `ReadOnlySqlQueryRunner`
(`src/HRManagement.Infrastructure/Persistence/ReadOnlySqlQueryRunner.cs:17-61`) bu bağlantıyı
kullanıp sonucu satır sayısı sınırıyla (`MaxRows`) kırpıyor — model `TOP` koymayı unutsa bile
bağlam penceresi patlamıyor.

**Karar Application'da, sağlayıcı Infrastructure'da:** `IAiAssistant`
(`src/HRManagement.Application/Interfaces/IAiAssistant.cs:45-64`) arayüzü hangi model/sağlayıcının
arkada olduğunu bilmiyor — sadece `AskAsync(systemPrompt, question, history, tools, executeTool,
ct)` imzasını sunuyor. Gerçek Anthropic SDK çağrısı `ClaudeAssistant`
(`src/HRManagement.Infrastructure/Ai/ClaudeAssistant.cs`) içinde — sağlayıcı değişse (başka model,
yerel bir model) yalnızca bu dosya değişir, `JwtTokenGenerator` ile aynı ilke (bkz. bölüm 1).

**Kim hangi aracı çalıştırıyor kararı Application'da kalıyor:** `AskAssistantQueryHandler`
(`src/HRManagement.Application/Features/Assistant/Queries/AskAssistant/AskAssistantQueryHandler.cs`)
tek aracı (`run_sql`) tanımlıyor, `ExecuteToolAsync`
(`AskAssistantQueryHandler.cs:120-164`) modelin çağırdığı aracı **gerçekten çalıştırıyor** —
önce `SqlReadOnlyGuard.IsReadOnly` kontrolü, sonra `ISqlQueryRunner.RunReadOnlyAsync`. `IAiAssistant`
arayüzünün belgesi bunu açıkça vurguluyor: "bu arayüz 'kim soruyor' bilgisini TAŞIMAZ. İstekçinin
kimliği araç yürütücüsünün closure'ında, imzalı JWT claim'inden gelen değerle sabitlenir. Model
kendi kimliğini bildiremez, dolayısıyla 'ben Admin'im' diyerek yetki yükseltemez." Ayrıca rol
kontrolü de handler'da: yalnızca `Role.HR` veya `Role.Admin`
(`AskAssistantQueryHandler.cs:75-76`) — çünkü serbest SELECT, satır bazlı görünürlük kuralını
(`EmployeeVisibility`) atlıyor; model tüm tabloyu görebiliyor, bu yüzden zaten her şeyi görebilen
rollere açık.

**Neden tek katman yetmiyor — somut senaryo:** Metin denetimi yalnızca **regex** ile çalışıyor;
teoride yeni bir atlatma tekniği (ör. denetimde düşünülmemiş bir T-SQL sözdizimi hilesi) ortaya
çıkabilir. Ama o durumda bile ikinci katman (salt-okuma DB kullanıcısı) devrede: `db_datareader`
rolündeki bir kullanıcı fiziksel olarak `INSERT`/`UPDATE` çalıştıramaz, SQL Server bunu reddeder —
kodda bir hata olsa bile veri değişmez.

**Alternatifi ve farkı:**

| Yaklaşım | Güvenlik | Esneklik |
|---|---|---|
| Bu proje (serbest SELECT + iki katman) | Metin + DB yetkisi ile savunma derinliği | Her soruya cevap verebilir |
| Önceden tanımlı sorgu şablonları (parametreli) | En güvenli — model hiç SQL yazmaz | Kapsam şablonlarla sınırlı, yeni soru türü için kod yazımı gerekir |
| Yalnızca metin denetimi (DB yetkisi olmadan) | Tek nokta arıza — regex atlatılırsa savunma biter | — |

**Bedeli / dezavantajı:** Serbest SELECT modeli, doğruluğu tamamen modelin ürettiği SQL'in
kalitesine bağlıyor — yanlış bir JOIN ya da eksik bir WHERE koşulu yanlış ama "çalışan" bir
sonuç üretebilir, sistem bunu otomatik yakalayamaz. Ayrıca satır bazlı görünürlük
(`EmployeeVisibility`) bu yol için devre dışı — bilinçli olarak yalnızca zaten her şeyi görebilen
rollere (HR/Admin) açılarak bu risk sınırlanıyor, ama başka bir role açılmak istenirse önce bu
görünürlük sorunu ayrıca çözülmeli (`AskAssistantQueryHandler.cs:66-69` yorumu bunu açıkça
işaretliyor).

**Karşı soru gelirse:** "Model 'DROP TABLE' yerine dolaylı bir yolla (ör. çok büyük bir sorguyla
DB'yi kilitleme) zarar verebilir mi?" — Kısmi önlem var: `CommandTimeoutSeconds = 15`
(`ReadOnlySqlQueryRunner.cs:20`) uzun süren sorguları kesiyor, `MaxToolIterations = 6`
(`ClaudeAssistant.cs:27`) modelin sonsuz araç döngüsüne girmesini engelliyor. Tam bir DoS
koruması değil ama "kaçak sorgu tüm veritabanını kilitlemesin" hedefiyle bilinçli sınırlar
konmuş.

---

## Hızlı tekrar kartları

- **Clean Architecture:** Bağımlılık yönü `.csproj` referanslarıyla derleyicide kilitli; Domain
  hiçbir şeye bağımlı değil, WebUI hiçbir iş katmanına bağımlı değil.
- **CQRS + MediatR:** Her use-case kendi mesajı + handler'ı; `ValidationBehavior` ortak kaygıyı
  tek yerde topluyor; sürüm 12'de sabit çünkü 13+ ticari lisansa geçti.
- **Dapper vs EF Core:** SQL'i sen yazıyorsun (kontrol + öğrenme), karşılığında change
  tracking/migration/refactor güvenliğinden elle vazgeçiyorsun.
- **MSSQL vs MongoDB:** İK verisi ilişkisel (çalışan→departman→yönetici zinciri); özyinelemeli CTE
  ile tek sorguda çözülen bir gezinme, MongoDB'de ya `$graphLookup` ya da uygulamada N+1 döngü
  olurdu.
- **Cookie + JWT:** Tarayıcı↔WebUI cookie (HttpOnly, JS okuyamaz), WebUI↔API JWT
  (`BearerTokenHandler` sunucu tarafında ekliyor); token tarayıcıya hiç sızmıyor.
- **Refit:** Arayüz + attribute'tan HTTP istemcisi üretiliyor; `ExceptionFactory` kapalı çünkü
  API hatası da zaten `BaseResponse` gövdesinde geliyor.
- **FluentValidation:** Koşullu kurallar (`.When`) DataAnnotations'ın zor ifade ettiği şey;
  `ValidationBehavior` ile otomatik, handler'a input-if yazılmıyor.
- **BaseResponse vs ProblemDetails:** Tek şekil, üç mekanizma (exception handler + model state
  factory + status code middleware) birlikte deliksiz tutuyor.
- **Contracts projesi yok:** Gevşek bağlılık kazanımı, JSON şeklini elle senkron tutma bedeli —
  `EmployeeResponse` iki host'ta iki ayrı dosya.
- **MVC vs SPA:** Sayfa bazlı iş akışı + cookie tabanlı kimlik doğrulamayla doğal örtüşüyor; SPA
  olsaydı token tarayıcıya sızar, CORS zorunlu olurdu.
- **BCrypt:** Kasıtlı yavaş + otomatik salt; SHA256 hızlı olduğu için brute-force'a açık, parola
  saklamak için yanlış araç.
- **AI asistanı:** Karar Application'da (`IAiAssistant` soyutlaması), sağlayıcı Infrastructure'da
  (`ClaudeAssistant`); iki katmanlı SQL güvenliği (metin denetimi + salt-okuma DB kullanıcısı) —
  biri atlatılırsa diğeri duruyor.
