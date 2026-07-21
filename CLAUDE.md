# HRManagement — Proje Rehberi

## Proje nedir
Clean Architecture ile yazılmış İK yönetim uygulaması (mentor eşliğinde ilerleyen bir öğrenme projesi).
Modüller: Auth + roller (Admin, HR, Manager, Employee, Intern), çalışan yönetimi,
izin talepleri (LeaveRequest), stajyer modülü, role-based dashboard'lar.

## Teknoloji
- .NET 10 / C#
- ASP.NET Core Web API (backend) + ASP.NET Core MVC (WebUI — server-rendered Razor)
- MediatR 12.x — SÜRÜM SABİT; 13+ ticari lisansa geçti, upgrade ETME
- FluentValidation — command/query input validation (pipeline behavior ile otomatik)
- Dapper — EF Core YOK ve EKLENMEYECEK
- Auth: tarayıcı↔WebUI = cookie authentication; WebUI↔API = Bearer JWT
- Testler: tests/HRManagement.Application.Tests (handler birim testleri)

## Solution yapısı
```
src/
├── HRManagement.Domain          — entity'ler, enum'lar, domain exception'ları
├── HRManagement.Application     — use-case'ler (MediatR), iş kuralları, repository kontratları
├── HRManagement.Infrastructure  — Dapper repo'ları, ConnectionFactory, JwtService (token ÜRETİMİ)
├── HRManagement.API             — API controller'lar, Models/ (request-response + BaseResponse), JWT doğrulama
└── HRManagement.WebUI           — MVC: Controllers, Views, ViewModels, Models/Api, Services (ApiService'ler)
tests/
└── HRManagement.Application.Tests
```

## Referans kuralları (ihlal edilemez)
- Paylaşılan Contracts projesi YOK (mentor kararı, 2026-07-20). Her host kendi modelini tutar:
  API → API/Models, WebUI → WebUI/Models/Api. Gevşek bağlılık; bedeli, JSON şeklinin
  iki tarafta elle senkron tutulması.
- WebUI hiçbir iş katmanına referans VERMEZ. Application/Domain/Infrastructure'a ASLA.
- API → Application + Infrastructure. Composition root = API/Program.cs.
- Infrastructure → Application → Domain. Domain → hiçbir şey.
- WebUI ile API arasında proje referansı YOK; iletişim yalnızca çalışma anında HTTP
  (typed HttpClient + WebUI/Services altındaki XxxApiService sınıfları).

## CQRS / MediatR konvansiyonları
- Saf MediatR: IRequest<T> / IRequestHandler<TReq, TRes>. Özel marker interface YOK.
- Klasörleme: Application/Features/{Modül}/{Commands|Queries}/{Operasyon}/
  → XxxCommand.cs + XxxCommandHandler.cs + XxxCommandValidator.cs yan yana durur.
- Handler imzası: Handle(TReq, CancellationToken) — CancellationToken repository'lere
  kadar iletilir, hiçbir katmanda yutulmaz.
- Input validation Validator'dadır; ValidationBehavior pipeline'da otomatik çalıştırır.
  Handler içine input-validation if'i yazılmaz.
- İş kuralı sonuçları Result / Result<T> ile döner (exception ile değil).
  Her iş kuralı ayrı private method olur (ör. izin: hak kontrolü → 6 ay kıdem → tarih çakışması).
- Query'ler yan etkisizdir; sistemde değişiklik yapan her şey Command'dır.

## API kuralları
- Global fallback authorization policy: TÜM endpoint'ler kilitli doğar; yalnızca login
  gibi bilinçli uçlar [AllowAnonymous] alır. Rol kontrolü: [Authorize(Roles = "...")].
- Yanıtlar BaseResponse<T> zarfıyla döner (mentor kararı): { IsSuccess, Message, Data }.
  Kullanım: BaseResponse<T>.Success(data) / BaseResponse<T>.Fail("mesaj").
- Hata yanıtları da AYNI zarfı kullanır (ProblemDetails KULLANILMIYOR): ValidationException
  → 400 + BaseResponse.Fail, beklenmeyen → 500 + BaseResponse.Fail, bulunamadı → 404 +
  BaseResponse.Fail. Tek format sayesinde istemci (Refit) tek tip deserialize eder.
- Controller incedir: API/Models altındaki request'i Command/Query'ye çevirir, ISender.Send
  eder, sonucu API/Models response'una çevirip döner. İş mantığı controller'a yazılmaz.
- Domain entity'si veya Application Command/Query tipi response'a sızmaz.

## WebUI kuralları
- API çağrıları **Refit** ile yapılır: Services/ altında yalnızca `IXxxApi` arayüzü tanımlanır
  ([Get]/[Post]/[Put]/[Delete] attribute'larıyla), implementasyonu Refit üretir.
  Elle HttpClient/JSON kodu YAZILMAZ. Kayıt: Program.cs'te AddRefitClient.
- RefitSettings.ExceptionFactory kapalıdır (null döner); API hata gövdesi de BaseResponse
  olduğu için exception yerine IsSuccess=false okunur.
- Controller'lar Application'ı DEĞİL, Services/ altındaki Refit arayüzlerini çağırır.
- BearerTokenHandler (DelegatingHandler), cookie ticket'ındaki JWT'yi her API isteğine ekler.
- API cevapları: 400 → ModelState'e maplenir; 401 → login redirect; 403 → AccessDenied sayfası.
- WebUI'daki her kontrol (validation, rol bazlı menü gizleme) UX içindir; otorite her
  zaman API + Application'dır.

## Güvenlik
- JWT secret YALNIZCA API tarafında yaşar; dotnet user-secrets ile yönetilir,
  koda ve git'e asla yazılmaz.
- Token tarayıcıya sızdırılmaz (JS'e/localStorage'a verilmez); cookie'ler HttpOnly.
- CORS eklenmez — WebUI→API çağrıları sunucudan yapılır, tarayıcıdan değil.

## Çalışma tarzı
- Küçük, doğrulanabilir adımlarla ilerle; her değişiklikte dosya listesi + kısa gerekçe ver.
- dotnet build ve testler yeşil kalmadan yeni işe geçme.
- Yeni NuGet bağımlılığı eklemeden önce sor.
- Var olan mimari kuralla çelişen bir istek gelirse önce çelişkiyi söyle.