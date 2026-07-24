using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;

/// <summary>
/// Bekleyen bir hesap talebini onaylar: hesabı açar, kişiye bağlar, talebi kapatır.
/// Yalnızca Admin. ApproverUserId claim'den gelir. Şifre BURADA belirlenir (talepte tutulmaz).
///
/// Rol SEÇİLMEZ: talebin rolü kullanılır — o rol de kişinin kıdeminden/türünden
/// türetilmiştir (bkz. AccountRoleResolver). Onayda rol override YOK.
/// </summary>
public sealed record ApproveAccountRequestCommand(
    int Id,
    int ApproverUserId,
    string Username,
    string Email,
    string Password) : IRequest<int>;
