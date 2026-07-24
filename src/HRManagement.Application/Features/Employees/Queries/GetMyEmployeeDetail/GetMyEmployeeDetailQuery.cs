using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetMyEmployeeDetail;

/// <summary>
/// "Profilim": isteği yapan hesabın KENDİ çalışan kaydının detayı. Kimlik
/// imzalı token'dan gelir (API CurrentUserId), gövdeden asla — kişi başkasının
/// profilini bu yoldan isteyemez. Hesap bir çalışana bağlı değilse null döner
/// (ör. Admin veya çalışan kaydı olmayan HR hesabı).
/// </summary>
public sealed record GetMyEmployeeDetailQuery(int RequesterUserId)
    : IRequest<EmployeeDetailDto?>;
