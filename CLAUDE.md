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
├── HRManagement.Contracts       — API ↔ WebUI ortak request/response DTO'ları (saf, referanssız)
├── HRManagement.API             — API controller'lar, JWT doğrulama, ProblemDetails
└── HRManagement.WebUI           — MVC: Controllers, Views, ViewModels, Services (ApiService'ler)
tests/
└── HRManagement.Application.Tests
```

## Referans kuralları (ihlal edilemez)
- WebUI → SADECE Contracts. Application/Domain/Infrastructure'a ASLA referans verme.
- Contracts → hiçbir projeye referans vermez.
- API → Application + Infrastructure + Contracts. Composition root = API/Program.cs.
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
- Tüm hatalar ProblemDetails formatında: validation → 400 (alan hatalarıyla),
  bulunamadı → 404, kimliksiz → 401, yetkisiz → 403.
- Controller incedir: Contracts request'ini Command/Query'ye çevirir, ISender.Send eder,
  sonucu Contracts response'una çevirip döner. İş mantığı controller'a yazılmaz.
- Domain entity'si veya Application Command/Query tipi Contracts'a/response'a sızmaz.

## WebUI kuralları
- Controller'lar Application'ı DEĞİL, Services/ altındaki XxxApiService'leri çağırır.
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