using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeDetail;

/// <summary>
/// Bir çalışanın detay/profil görünümü. RequesterUserId görünürlük (kim kimi
/// görebilir) ve alan kırpma (TC, izin açıklaması) kararlarında kullanılır.
/// </summary>
public sealed record GetEmployeeDetailQuery(int EmployeeId, int RequesterUserId)
    : IRequest<EmployeeDetailDto?>;
