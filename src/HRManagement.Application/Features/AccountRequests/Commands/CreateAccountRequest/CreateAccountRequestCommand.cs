using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.CreateAccountRequest;

/// <summary>
/// "Şu kişiye hesap açılsın" talebi. HR (veya Admin) oluşturur.
/// RequestedByUserId imzalı JWT claim'inden gelir (denetim: kim talep etti),
/// istek gövdesinden ASLA. EmployeeId/InternId'den tam olarak biri dolu olmalı.
/// </summary>
public sealed record CreateAccountRequestCommand(
    int RequestedByUserId,
    int? EmployeeId,
    int? InternId,
    Role SuggestedRole,
    string? Note) : IRequest<int>;
