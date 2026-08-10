# AI Asistan Akışı — Baştan Sona

Bu doküman, "İK'nın veriye doğal dille soru sorabildiği asistan" özelliğinin
**tek bir sorunun** tarayıcıdan çıkıp veritabanına gidip cevaba dönmesine kadar
izlediği yolu anlatır. Her bölüm bir durak; sonda tam bir tur adım adım izlenir.

Okuma sırası önemli — bölümler birbirinin üstüne kuruluyor.

---

## İçindekiler

| # | Bölüm | Soru |
|---|---|---|
| 0 | [Harita](#0-harita) | Genel resim neye benziyor? |
| 1 | [Tarayıcı](#1-tarayıcı--soru-nasıl-gönderiliyor) | Soru nereden çıkıyor? |
| 2 | [WebUI köprüsü](#2-webui-köprüsü--neden-araya-bir-katman-var) | Neden doğrudan API'ye gitmiyor? |
| 3 | [API ucu](#3-api-ucu--kimlik-burada-sabitlenir) | "Kim soruyor" nerede belirleniyor? |
| 4 | [Validator](#4-validator--girdi-sınırları) | Girdi nasıl sınırlanıyor? |
| 5 | [Handler](#5-handler--asistanın-beyni) | Kararları kim veriyor? |
| 6 | [Sistem prompt'u](#6-sistem-promptu--modele-ne-öğretiliyor) | Model şemayı nereden biliyor? |
| 7 | [Döngü](#7-döngü--claudeassistant) | Model ile araç nasıl konuşuyor? |
| 8 | [Araç çalıştırma](#8-araç-çalıştırma--guard--runner) | SQL nasıl güvenli çalışıyor? |
| 9 | [Sohbet geçmişi](#9-sohbet-geçmişi) | Model önceki soruyu nasıl hatırlıyor? |
| 10 | [Tam bir tur](#10-tam-bir-tur--adım-adım) | Hepsi birlikte nasıl işliyor? |
| 11 | [Güvenlik katmanları](#11-güvenlik-katmanları) | Kaç kapı var? |
| 12 | [Bilinen eksikler](#12-bilinen-eksikler) | Neresi hâlâ açık? |

---

## 0. Harita

```
┌─ TARAYICI ────────────────────────────────────────────────────┐
│  _AssistantWidget.cshtml                                      │
│  kullanıcı soruyu yazar → fetch POST /Assistant/Ask           │
└───────────────────────────┬───────────────────────────────────┘
                            │  cookie (JWT içeride, JS göremez)
┌─ WebUI ────────────────────▼──────────────────────────────────┐
│  Controllers/AssistantController.cs   [Authorize HR,Admin]    │
│  Services/IAssistantApi.cs  (Refit)                           │
│  Services/BearerTokenHandler.cs  → Authorization: Bearer ...   │
└───────────────────────────┬───────────────────────────────────┘
                            │  HTTP + JWT
┌─ API ──────────────────────▼──────────────────────────────────┐
│  Controllers/AssistantController.cs   [Authorize HR,Admin]    │
│  ActorUserId = token'daki NameIdentifier claim'i              │
└───────────────────────────┬───────────────────────────────────┘
                            │  MediatR
┌─ APPLICATION ──────────────▼──────────────────────────────────┐
│  AskAssistantQueryValidator      girdi sınırları              │
│  AskAssistantQueryHandler        BEYİN: yetki, araç, geçmiş   │
│  HrDatabaseSchema                sistem prompt'u              │
│  SqlReadOnlyGuard                SQL denetimi                 │
└──────────┬─────────────────────────────────┬──────────────────┘
           │ IAiAssistant                    │ ISqlQueryRunner
┌─ INFRASTRUCTURE ─▼──────────────┐   ┌──────▼──────────────────┐
│  Ai/ClaudeAssistant.cs          │   │ ReadOnlySqlQueryRunner  │
│  model ↔ araç DÖNGÜSÜ           │   │ salt okuma bağlantı     │
│  Ai/MemoryConversationStore.cs  │   └──────┬──────────────────┘
└──────────┬──────────────────────┘          │
           │ HTTPS                            │ SQL
      ┌────▼─────┐                      ┌─────▼──────┐
      │ Anthropic│                      │ SQL Server │
      └──────────┘                      └────────────┘
```

**Akılda tutulacak tek cümle:** Karar veren Application, konuşan Infrastructure.
`ClaudeAssistant` hangi aracın ne yaptığını bilmez; `AskAssistantQueryHandler`
modelin nasıl konuştuğunu bilmez.

---

## 1. Tarayıcı — soru nasıl gönderiliyor

**Dosya:** `src/HRManagement.WebUI/Views/Shared/_AssistantWidget.cshtml`

Sağ altta bir düğme, açılınca sohbet paneli. Kullanıcı soruyu yazıp gönderdiğinde
JS bir `fetch` POST'u atar:

```
POST /Assistant/Ask
  question       = "Bilgi Teknolojileri'nde kaç aktif çalışan var?"
  conversationId = "8f3a1c2e-..."   (tarayıcıda üretilen UUID)
  __RequestVerificationToken = ...  (CSRF)
```

### Bilinmesi gereken üç şey

**`conversationId` tarayıcıda üretiliyor.** "Yeni sohbet" düğmesi yeni bir UUID
üretir — sunucudaki eski kayıt silinmez, kendi süre aşımıyla düşer. Ona artık
kimse ulaşamaz çünkü kimliği hiçbir yerde tutulmaz.

**Döküman `sessionStorage`'da tutulur ama HTML olarak değil, VERİ olarak.**
Sayfa yenilenince sohbet geri çizilir; çizim yine aynı `md()`/`esc()` hattından
geçer. Kaçış tek yerde kalır.

**Model çıktısı güvenilmez kabul edilir.** Cevap ekrana basılmadan önce
kaçırılır, sonra biçimlendirilir:

```js
const esc = s => String(s).replace(/[&<>"']/g, c => ({...}[c]));
function md(src) {
    const L = esc(src).split('\n');   // ÖNCE kaçış, SONRA markdown
    ...
}
```

Sıra ters olsaydı cevaba gömülü bir `<script>` çalışırdı. Model veritabanındaki
metinleri okuyor ve o metinleri herhangi bir çalışan yazabiliyor — yani bu
gerçek bir saldırı yüzeyi, teorik değil.

---

## 2. WebUI köprüsü — neden araya bir katman var

**Dosyalar:**
- `src/HRManagement.WebUI/Controllers/AssistantController.cs`
- `src/HRManagement.WebUI/Services/IAssistantApi.cs`
- `src/HRManagement.WebUI/Services/BearerTokenHandler.cs`

Panel neden doğrudan API'ye gitmiyor? **Çünkü JWT tarayıcıda yok.**

Token, cookie ticket'ının **içinde** sunucuda duruyor. JS onu okuyamaz
(`HttpOnly`), `localStorage`'a hiç yazılmaz. Dolayısıyla tarayıcı API'ye
kendi başına kimlik sunamaz. İstek WebUI'dan geçmek zorunda ki
`BearerTokenHandler` token'ı ekleyebilsin:

```csharp
var token = await httpContext.GetTokenAsync("access_token");
if (!string.IsNullOrWhiteSpace(token))
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

WebUI controller'ı iş yapmaz — Refit arayüzüne devreder ve dönen `BaseResponse`'u
panelin beklediği JSON'a çevirir:

```csharp
[Authorize(Roles = "HR,Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Ask([FromForm] string question, [FromForm] string conversationId)
```

> **Not:** Buradaki `[Authorize]` **UX içindir** — kullanıcıyı boşuna
> bekletmemek için. Gerçek kapı API'dedir. `IAssistantApi`'nin kendi yorumu da
> bunu söylüyor.

Bir de zaman aşımı ayrıntısı: asistan istemcisine `Program.cs`'te özel olarak
**3 dakika** verilmiş, çünkü modelin sorguyu yazması + çalıştırması varsayılan
100 saniyeyi aşabilir.

---

## 3. API ucu — kimlik burada sabitlenir

**Dosya:** `src/HRManagement.API/Controllers/AssistantController.cs`

```csharp
[Authorize(Roles = "HR,Admin")]
[HttpPost("ask")]
public async Task<IActionResult> Ask([FromBody] AskAssistantRequest request)
{
    var result = await _mediator.Send(
        new AskAssistantQuery(request.Question, request.ConversationId, CurrentUserId()));
    ...
}

private int CurrentUserId() =>
    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

**Bu dosyadaki en önemli satır `CurrentUserId()`.**

`ActorUserId` istek gövdesinden **alınmaz**, imzalı token'dan okunur. İstemci
gövdeye `"actorUserId": 1` yazarak Admin olduğunu iddia edemez — o alan zaten
gövdede yok.

Neden rol kısıtı sadece HR ve Admin? Çünkü asistan **serbest SELECT**
çalıştırıyor. Satır bazlı görünürlük kuralları (`EmployeeVisibility`,
`MentorshipGuard`) devre dışı kalıyor — model tabloların tamamını okuyabiliyor.
Bu yüzden özellik yalnızca **zaten her şeyi görebilen** rollere açık.

---

## 4. Validator — girdi sınırları

**Dosya:** `AskAssistantQueryValidator.cs`

Projedeki **ilk query validator'ı**. Diğer query'lerin girdisi route'tan gelen
`int` ve JWT claim'i olduğu için doğrulanacak bir şey yoktu; burada gerçek bir
kullanıcı metni var.

| Kural | Sebep |
|---|---|
| `Question` boş olamaz | — |
| `Question` en fazla **500 karakter** | Token maliyeti + prompt injection yüzeyi |
| `ConversationId` boş olamaz | Depo anahtarının parçası |
| `ConversationId` `^[A-Za-z0-9-]{1,64}$` | Serbest metin anahtar alanını kirletir |
| `ActorUserId > 0` | — |

`ValidationBehavior` pipeline'da otomatik çalışır; handler'a hiç girilmeden
reddedilir.

---

## 5. Handler — asistanın beyni

**Dosya:** `AskAssistantQueryHandler.cs`

Sınıfın kendi yorumu özeti veriyor:

> *Asistanın beyni Application'dadır: hangi aracın var olduğuna, çalıştırılıp
> çalıştırılmayacağına ve kimin sorabileceğine BURASI karar verir. Infrastructure
> yalnızca "modelle konuşmayı" bilir.*

### 5.1. İkinci yetki kapısı

```csharp
var actor = await _userRepository.GetByIdAsync(request.ActorUserId);

if (actor is null || !actor.IsActive)
    throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

if (actor.Role is not (Role.HR or Role.Admin))
    throw new ValidationException("Asistanı kullanma yetkiniz yok.");
```

Controller'da zaten `[Authorize(Roles = "HR,Admin")]` var — neden tekrar?

**Çünkü JWT claim'i bayatlayabilir.** Token 2 saat geçerli. Bir kullanıcının
rolü düşürülse ya da hesabı pasife alınsa, elindeki token 2 saat daha "HR"
demeye devam eder. Handler rolü **veritabanından** tekrar okuyarak bu boşluğu
kapatır.

### 5.2. Tek araç

```csharp
private static readonly AiTool SqlTool = new(
    Name: "run_sql",
    Description: "İK veritabanında SALT OKUMA T-SQL SELECT sorgusu çalıştırır...",
    InputSchemaJson: """
        { "type": "object",
          "properties": { "query": { "type": "string", ... } },
          "required": ["query"] }
        """);
```

Bilinçli olarak **tek** araç. Alternatif, önceden tanımlı sorgu listesi
sunmaktı ("çalışan sayısını getir", "izinleri listele"). Serbest SELECT tercih
edildi çünkü amaç "her veritabanı sorusuna cevap".

- **Bedeli:** doğruluk modele bağlı.
- **Karşılığı:** kapsam şemayla sınırlı, yeni soru tipi için kod yazılmıyor.

### 5.3. Geçmiş ve kesme notu

```csharp
var history = _conversationStore.Get(request.ActorUserId, request.ConversationId);

var prompt = history.DroppedTurnCount > 0
    ? HrDatabaseSchema.TruncationNotice(history.DroppedTurnCount) + request.Question
    : request.Question;
```

Pencereden tur düştüyse modele haber verilir. **Not sistem prompt'una değil
soruya ekleniyor** — sistem prompt'u önbelleğe alınmış durumda, tek baytı
değişirse önbellek komple düşer.

Ve geçmişe **kullanıcının orijinal sorusu** yazılır, not eklenmiş hâli değil:

```csharp
_conversationStore.Append(request.ActorUserId, request.ConversationId,
    new ConversationTurn(request.Question, answer));
```

Aksi hâlde not sohbette kalıcılaşır ve her turda tekrar tekrar birikir.

---

## 6. Sistem prompt'u — modele ne öğretiliyor

**Dosya:** `Features/Assistant/Shared/HrDatabaseSchema.cs`

Bu dosya bir `const string` — yaklaşık 2500 token. Dört bölümü var:

### 6.1. MUTLAK KURALLAR
"Yalnızca araçtan dönen veriye dayan", "asla sayı uydurma", "her sorguya
`SELECT TOP 200` koy", "veri yetmiyorsa aracı hiç çağırma".

### 6.2. TABLOLAR
Şema listesi. Dikkat: `Employees` satırında **`NationalId` bilerek yok** —
T.C. kimlik yalnızca İK'ya açık, asistan ise İK **+ Admin**'e açık. Kolonu
tanıtmamak bir kontroldür; "sorgulama" diye not düşmek yalnızca tavsiyedir.

### 6.3. SAYISAL KOD KARŞILIKLARI
`Role`, `Seniority`, `Gender`, `LeaveRequests.Type`, `Status`... Veritabanı
sayı tutuyor, metin karşılıkları yok. Model bunları bilmezse "Status = 3" der,
"Onaylandı" diyemez.

### 6.4. TUZAKLAR — en kritik bölüm
Şemayı vermek **yetmez**. Bu domain'de veritabanına bakarak anlaşılamayacak
kurallar var. En tehlikelisi:

> **Yıllık izin hakkı veritabanında kolon olarak YOKTUR — hesaplanır.**
> `Employees.AnnualLeaveDays` neredeyse her zaman NULL'dur.

Model bunu bilmezse `SELECT AnnualLeaveDays` yazar, NULL alır ve "hakkı yok"
der — **çalışan ama yanlış** bir cevap. En kötü hata türü.

Bu yüzden prompt'ta hazır bir SQL şablonu var (kıdem hesabı + kullanılan izin
toplamı, `CROSS APPLY` zinciriyle). Model formülü baştan yazmaya çalışmaz.

Diğer tuzaklar: kullanılan izin = yalnızca `Type=1 AND Status IN (1,2,3)`;
talep sahibi ya `EmployeeId` ya `InternId` (ikisi asla dolu değil); "Pozisyon"
diye kolon yok; stajyerde `IsActive` yok; tarihler UTC.

> **Bedel:** Şema değişirse (yeni `db/` script'i) bu dosya da elle
> güncellenmelidir. Bilinçli kabul edilmiş bir bağımlılık.

---

## 7. Döngü — ClaudeAssistant

**Dosya:** `src/HRManagement.Infrastructure/Ai/ClaudeAssistant.cs`

Bu sınıf **hangi aracın ne yaptığını bilmez**. Modelin "şunu çağır" dediğini
alıp Application'ın verdiği geri çağırıma iletir.

### 7.1. İskelet

```csharp
for (var iteration = 0; iteration < MaxToolIterations; iteration++)   // en fazla 6
{
    var response = await SendAsync(systemBlocks, anthropicTools, messages);

    if (response.StopReason == "refusal")   return RefusalMessage;
    if (response.StopReason != "tool_use")  return ExtractText(response);

    var (assistantContent, toolUses) = SplitContent(response);
    var toolResults = await RunToolsAsync(toolUses, executeTool, cancellationToken);

    messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });
    messages.Add(new MessageParam { Role = Role.User,      Content = toolResults });
}
```

Okunuşu: **gönder → reddetti mi → araç mı istedi → çalıştır → geri gönder → tekrarla.**

`MaxToolIterations = 6`: model bir sorguyu düzeltmek için birkaç deneme
yapabilmeli (önce departman listesini çeker, sonra asıl sorguyu yazar) ama
sonsuz döngüye girmemeli.

### 7.2. Mesaj bir string değil, blok listesi

Modelin tek turu aynı anda birden fazla şey içerebilir:

```
Asistan turu:
  content: [ ThinkingBlock, TextBlock, ToolUseBlock, ToolUseBlock ]
```

Bu yüzden `content` bir dizi ve elemanların ortak tipi `ContentBlockParam`.

**İsimlendirme kuralı:** `Block` = aldığın, `BlockParam` = gönderdiğin.

| Aldığın | Gönderdiğin |
|---|---|
| `TextBlock` | `TextBlockParam` |
| `ThinkingBlock` | `ThinkingBlockParam` |
| `ToolUseBlock` | `ToolUseBlockParam` |
| — | `ToolResultBlockParam` |

Alt satır kuralı kanıtlıyor: araç **sonucunu** yalnızca sen gönderirsin, hiç
almazsın — o yüzden `Param`sız ikizi yok.

### 7.3. Neden blokları kopyalıyoruz

```csharp
if (block.TryPickText(out TextBlock? text))
    assistantContent.Add(new TextBlockParam { Text = text.Text });
```

Anlamsız görünür ama **aldığın tipten gönderdiğin tipe** geçiş. Model bir araç
istediğinde konuşmanın devam etmesi için o turu **aynen** geri göndermen gerekir
— model kendi söylediğini görmeli.

Düşünce bloklarının **imzası aynen korunmalı**; kurcalanmış bir düşünce bloğunu
API reddeder.

### 7.4. Neden araç sonucu "user" mesajı

```csharp
messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
```

API'de iki rol var: `assistant` (model) ve `user` (**modelin dışındaki her şey**).
Araç sonucu modelden gelmediği için `user` tarafına yazılır.

Ve **hepsi tek mesajda** gitmeli: model iki araç istediyse, hemen sonraki tek
user mesajı iki `ToolResultBlockParam` içermeli. Her sonuç `ToolUseID` ile hangi
çağrıya cevap olduğunu söyler. Birini ayrı mesaja koyarsan API "bir tool_use'a
cevap gelmemiş" der ve isteği reddeder.

### 7.5. Önbellek

```csharp
new() { Text = systemPrompt, CacheControl = new CacheControlEphemeral() }
```

Sistem prompt'u + araç tanımları her istekte harfi harfine aynı ve büyük.
Önbellek işareti bunları bir kez işletip sonraki isteklerde temel fiyatın
~0.1'ine okutur; yazma 1.25×, yani **iki istekte başa baş**.

İşaret yalnızca `system`'e konuyor çünkü araçlar istemde system'den **önce**
yer alır — tek işaret ikisini birden kapsar.

---

## 8. Araç çalıştırma — guard + runner

Model `run_sql` dediğinde `ClaudeAssistant` değil, **Application'ın verdiği
geri çağırım** çalışır: `AskAssistantQueryHandler.ExecuteToolAsync`.

### 8.1. Adımlar

```csharp
if (toolName != SqlToolName)
    return $"HATA: '{toolName}' diye bir araç yok.";

if (!arguments.TryGetValue("query", out var queryElement) || ...)
    return "HATA: 'query' parametresi metin olarak verilmeli.";

var sql = queryElement.GetString() ?? string.Empty;

if (!SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out var reason))
    return $"HATA: Sorgu reddedildi — {reason}";

executedQueries.Add(safeSql);
var result = await _sqlQueryRunner.RunReadOnlyAsync(safeSql, cancellationToken);
```

**Hatalar exception FIRLATMAZ, metin olarak döner.** Sebep: model mesajı okuyup
kendini düzeltebilsin. Yasak kelime kullandıysa sorguyu yeniden yazar. Exception
sohbeti tamamen keserdi.

### 8.2. Guard'ın kritik invaryantı

`TryNormalize` iki metin üretir:

- **`executable`** — yorumları ayıklanmış, metin sabitleri korunmuş
- **`inspected`** — aynısı, ama string içerikleri boşaltılmış

`inspected` denetlenir, `executable` (`safeSql`) çalıştırılır. İkisi yalnızca
veri kısmında ayrışır, **yapıda birebir aynıdır.**

Bu neden hayati: eskiden denetlenen metin ile çalıştırılan metin **farklıydı**
ve bu bir açıktı. `SELECT '--' AS x; DROP TABLE EmployeeNotes` sorgusunda naif
yorum ayıklayıcı `--`'yi yorum sanıp gerisini siliyordu; denetime masum bir
`SELECT '` gidiyor, veritabanına iki ifadelik zincir gidiyordu.

**Kural:** *denetlenen metin = çalışan metin.*

### 8.3. İkinci katman

```csharp
using var connection = _connectionFactory.CreateReadOnlyConnection();
```

Sorgu **ayrı bir SQL kullanıcısıyla** (`db_datareader`) çalışır. Guard bir metin
denetimidir ve her metin denetimi atlatılabilir; veritabanı izni atlatılamaz.

Ek frenler: **15 saniye** komut zaman aşımı, **200 satır** üst sınır (model
`TOP` koymayı unutsa bile bağlam penceresi patlamasın).

---

## 9. Sohbet geçmişi

**Dosyalar:** `IConversationStore.cs`, `Infrastructure/Ai/MemoryConversationStore.cs`

| Ayar | Değer | Sebep |
|---|---|---|
| `MaxTurns` | 12 | Bağlam penceresi |
| `MaxAnswerChars` | 2000 | Tek tablo cevabı birkaç kısa turdan fazla yer kaplar |
| Süre | 30 dk **kayan** | Terk edilmiş sohbet kendini toplar, aktif olan kesilmez |

### 9.1. Anahtar

```csharp
private static string Key(int userId, string conversationId) =>
    $"asst:{userId}:{conversationId}";
```

**Kullanıcı kimliği anahtarın parçası.** Yalnızca `conversationId` ile
kurulsaydı, başkasının kimliğini tahmin eden onun sohbetini okurdu. Kimlik
JWT'den gelir, istemciden değil.

### 9.2. Neden sunucuda

Geçmiş her istekte modele gidiyor — yani doğrudan token maliyeti. Sınırı
**faturayı ödeyen taraf** koymalı. İstemciye bırakılsaydı aynı kuralı iki yerde
(JS'te ve C#'ta) yazmak ve senkron tutmak gerekirdi, üstelik istemciden gelen
sınıra zaten güvenilemezdi.

**Bedeli:** API yeniden başlayınca geçmiş uçar, birden fazla API örneğinde
paylaşılmaz. İkisi de kabul edildi — bu bir kolaylık, veri değil.

### 9.3. Araç turları saklanmaz

Geçmişe yalnızca **metin çiftleri** (soru, cevap) yazılır. Eski sorgu sonuçları
saklanmaz çünkü hem bağlamı şişirir hem **bayatlar** — model dünkü veriyle
bugünkü soruyu cevaplamamalı. Bağlamı görüp veriyi yeniden sorgulaması doğrusu.

---

## 10. Tam bir tur — adım adım

Soru: **"Bilgi Teknolojileri'nde kaç aktif çalışan var?"**

### Adım 1 — Tarayıcı
`fetch POST /Assistant/Ask` · question + conversationId + CSRF token

### Adım 2 — WebUI
Rol kontrolü (UX), `IAssistantApi.AskAsync` çağrılır, `BearerTokenHandler`
cookie ticket'ından JWT'yi çıkarıp başlığa koyar.

### Adım 3 — API
`[Authorize(Roles = "HR,Admin")]` geçilir. `CurrentUserId()` → örneğin `7`.
`AskAssistantQuery("Bilgi Teknolojileri'nde...", "8f3a1c2e-...", 7)`

### Adım 4 — Validator
500 karakter altında ✔ · conversationId biçimi ✔

### Adım 5 — Handler
Kullanıcı 7 veritabanından okunur → aktif ✔, rolü HR ✔
Geçmiş boş (`DroppedTurnCount = 0`) → kesme notu eklenmez.

### Adım 6 — İlk API çağrısı

```
system:   [HrDatabaseSchema.SystemPrompt]  (önbellek işaretli)
tools:    [run_sql]
messages: [ {user: "Bilgi Teknolojileri'nde kaç aktif çalışan var?"} ]
```

### Adım 7 — Modelin ilk cevabı

```
stop_reason: "tool_use"
content: [
  TextBlock    → "Bilgi Teknolojileri departmanını kontrol ediyorum."
  ToolUseBlock → id: "toolu_01A"
                 name: "run_sql"
                 input: { query: "SELECT TOP 200 COUNT(*) AS Sayi
                                  FROM Employees e
                                  JOIN Departments d ON d.Id = e.DepartmentId
                                  WHERE e.IsActive = 1
                                    AND d.Name LIKE '%Bilgi Teknolojileri%'" }
]
```

`SplitContent` bunu ikiye ayırır: geri gönderilecek 2 blok + çalıştırılacak 1
araç çağrısı.

### Adım 8 — Araç çalışır

1. `toolName == "run_sql"` ✔
2. `query` string ✔
3. `SqlReadOnlyGuard.TryNormalize` → geçer, `safeSql` üretilir
4. `executedQueries.Add(safeSql)`
5. Salt okuma bağlantıyla çalışır → `[{"Sayi":12}]`
6. Döner: `"1 satır:\n[{\"Sayi\":12}]"`

### Adım 9 — Mesajlar büyür

```
messages: [
  {user:      "Bilgi Teknolojileri'nde kaç aktif çalışan var?"},
  {assistant: [TextBlockParam, ToolUseBlockParam]},        ← modelin turu, aynen
  {user:      [ToolResultBlockParam(toolu_01A, "1 satır: ...")]}
]
```

### Adım 10 — İkinci API çağrısı

```
stop_reason: "end_turn"
content: [ TextBlock → "Bilgi Teknolojileri departmanında 12 aktif çalışan var." ]
```

`StopReason != "tool_use"` → döngü biter, `ExtractText` metni döndürür.

### Adım 11 — Geri dönüş

```
Handler   → geçmişe (soru, cevap) yazılır
          → AssistantAnswerDto { Answer, ExecutedQueries = [safeSql] }
API       → BaseResponse<AssistantAnswerResponse>.Success(...)
WebUI     → { ok: true, answer: "...", queries: [...] }
Tarayıcı  → md() ile kaçırılıp biçimlendirilir, balona basılır
            "1 sorgu çalıştırıldı" detayı açılabilir kutuda gösterilir
```

**Toplam: 2 model çağrısı, 1 veritabanı sorgusu, 1 döngü turu.**

---

## 11. Güvenlik katmanları

| # | Katman | Nerede | Neyi durdurur |
|---|---|---|---|
| 1 | Cookie `HttpOnly` | WebUI | JWT'nin JS'e sızmasını |
| 2 | CSRF token | WebUI | Başka siteden gönderilen isteği |
| 3 | `[Authorize(Roles)]` | WebUI + API | Yetkisiz rolü |
| 4 | Handler rol kontrolü | Application | **Bayat token'ı** (rol düşürüldü ama token geçerli) |
| 5 | Validator | Application | 500+ karakter, bozuk conversationId |
| 6 | Sistem prompt'u | Application | Model davranışını yönlendirir — **kontrol değil, tavsiye** |
| 7 | Şemadan kolon gizleme | Application | T.C. kimliği — **gerçek kontrol** |
| 8 | `SqlReadOnlyGuard` | Application | SELECT dışını, çok ifadeyi, sistem yordamlarını |
| 9 | Salt okuma DB kullanıcısı | Infrastructure | Guard atlatılsa bile **yazmayı** |
| 10 | 200 satır / 15 sn | Infrastructure | Kaçak sorgunun kaynak tüketmesini |
| 11 | Geçmiş anahtarında userId | Infrastructure | Başkasının sohbetini okumayı |
| 12 | `esc()` sonra `md()` | Tarayıcı | Model çıktısındaki XSS'i |

**6 ve 7 arasındaki fark en önemli ders:** prompt'a "şunu sorgulama" yazmak bir
**tavsiyedir**. Kolonu şemadan çıkarmak bir **kontroldür**. Model tavsiyeye
uymayabilir; bilmediği kolonu ise soramaz.

---

## 12. Bilinen eksikler

Bu bölüm dürüstlük için — sistemin bugünkü açıkları.

### 12.1. `;`siz ifade zincirleme (açık)
T-SQL ifadeler arasında `;` zorunlu tutmaz. `SELECT 1 SELECT 2` iki ifadedir ama
guard'ın çok-ifade denetimi yalnızca `;` arar → geçer.

**Ciddiyeti düşük:** salt okuma kullanıcısıyla ikinci bir `SELECT` fazladan yetki
vermez. Zarar verebilecek başlatıcılar (`DBCC`, `KILL`, `CHECKPOINT`, `USE`,
`GO`, `DECLARE`, `SET`...) yasak listesine eklendi. Ama guard kendi sözleşmesini
("tek ifade çalışır") tam tutmuyor.

**Yapısal çözüm:** parantez derinliği 0'daki ilk `SELECT` serbest, sonraki her
depth-0 `SELECT` yalnızca `UNION`/`EXCEPT`/`INTERSECT` sonrası gelebilir.

### 12.2. Dolaylı prompt injection (açık)
Asistanın okuduğu tablolarda (`LeaveRequests.Description`, `EmployeeNotes.Content`,
`InternTasks.Title`) **herhangi bir çalışan** metin yazabiliyor. İK biri "bu ayki
izin gerekçelerini özetle" dediğinde, o metinler modelin bağlamına girer ve
talimat içerebilir.

Bugünkü frenler: model yalnızca `run_sql` çağırabiliyor, guard SELECT dışını
reddediyor, DB kullanıcısı yazamıyor. Yani injection **komut çalıştıramaz** —
ama modeli yanlış özet üretmeye yönlendirebilir.

### 12.3. `CancellationToken` API çağrısına geçilmiyor
SDK kabul ediyor (`Messages.Create(params, CancellationToken)`), kod geçmiyor.
Kullanıcı sayfayı kapatsa bile istek Anthropic'e gitmeye ve **ücretlenmeye**
devam ediyor.

### 12.4. Paralel araç çağrıları sırayla çalışıyor
Model tek turda birden fazla `run_sql` isteyebilir; bunlar bağımsızdır ama
`RunToolsAsync` sırayla bekliyor. Paralelleştirmek için önce `executedQueries`
listesinin eşzamanlı yazmaya hazır hale gelmesi gerekir (şu an düz `List<string>`
— yarış durumu doğar).

### 12.5. Hız sınırı yok
`conversationId` istemciden geliyor; her yeni id yeni bir önbellek girdisi açar.
Kullanıcı başına dakikalık istek sınırı yok — her istek Anthropic'e ücret yazar.
Riski HR/Admin kısıtı düşürüyor ama kaldırmıyor.

### 12.6. Hata mesajı sızıntısı
SQL hatası modele ve dolaylı olarak kullanıcıya **aynen** dönüyor (kolon adları,
tablo adları). Bilinçli kabul edilmiş: uç yalnızca İK/Admin'e açık, ve model
hatayı okuyup sorguyu düzeltebiliyor.

---

## Özet — üç cümle

1. **Karar Application'da, konuşma Infrastructure'da.** `ClaudeAssistant` hangi
   aracın ne yaptığını bilmez; handler modelin nasıl konuştuğunu bilmez.
2. **Kimlik hiçbir noktada istemciden gelmez.** Token'dan okunur, veritabanından
   doğrulanır, geçmiş anahtarına gömülür. Model kendi kimliğini bildiremez.
3. **Güvenlik tek katmana yaslanmaz.** Metin denetimi atlatılabilir varsayılır;
   arkasında veritabanı izni durur. Prompt'taki talimat tavsiyedir, şemadan
   çıkarılan kolon kontroldür.
