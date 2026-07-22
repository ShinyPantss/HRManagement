using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;

/// <summary>
/// Bekleyen bir hesap talebini onaylar: hesabı açar, kişiye bağlar, talebi kapatır.
/// Yalnızca Admin. ApproverUserId claim'den gelir. Şifre BURADA belirlenir
/// (talepte tutulmaz). Role opsiyoneldir: verilmezse talebin SuggestedRole'ü kullanılır.
/// </summary>
public sealed record ApproveAccountRequestCommand(
    int Id,
    int ApproverUserId,
    string Username,
    string Email,
    string Password,
    Role? Role) : IRequest<int>;
