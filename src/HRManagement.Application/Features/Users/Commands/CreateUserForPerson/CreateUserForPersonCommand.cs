using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.CreateUserForPerson;

/// <summary>
/// "Var olan bir kişiye (çalışan VEYA stajyer) giriş hesabı aç" isteği.
/// Yalnızca Admin çağırır (rol atama gücü = birini Admin yapma gücü).
///
/// EmployeeId ve InternId'den TAM OLARAK BİRİ dolu olmalı: hesap kime açılıyorsa.
/// Kişi kaydını HR önceden oluşturur; UserId'si boş olması "hesap bekliyor"
/// anlamına gelir (ayrı bir talep tablosu yok, durum veriden türetilir).
///
/// Dönen değer yeni User'ın Id'sidir. Hesap oluşturma ve kişiye bağlama TEK
/// TRANSACTION'da yapılır — yarısı patlarsa sahipsiz hesap kalmaz.
/// </summary>
public sealed record CreateUserForPersonCommand(
    string Username,
    string Email,
    string Password,
    Role Role,
    int? EmployeeId,
    int? InternId) : IRequest<int>;
