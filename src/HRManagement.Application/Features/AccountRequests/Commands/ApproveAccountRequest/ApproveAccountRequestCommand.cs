using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;

/// <summary>
/// Bekleyen bir hesap talebini onaylar: hesabı açar, kişiye bağlar, talebi kapatır.
/// Yalnızca Admin. ApproverUserId claim'den gelir. Şifre BURADA belirlenir (talepte tutulmaz).
///
/// Role OPSİYONEL override: verilmezse (null) talebin türetilmiş rolü kullanılır.
/// Kıdemden TÜRETİLEMEYEN roller (İK, Admin) için Admin burada elle seçer —
/// ör. İK Müdürü hesabı = HR (izin onayında yöneticilik zaten org zincirinden gelir).
/// </summary>
public sealed record ApproveAccountRequestCommand(
    int Id,
    int ApproverUserId,
    string Username,
    string Email,
    string Password,
    Role? Role) : IRequest<int>;
