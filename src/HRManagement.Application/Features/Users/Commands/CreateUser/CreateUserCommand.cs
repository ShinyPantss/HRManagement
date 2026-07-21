using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// "Yeni kullanıcı ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner.
/// </summary>
public sealed record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    Role Role) : IRequest<int>;
