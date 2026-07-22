using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeById;

/// <summary>
/// Tek çalışanın detayı. RequesterUserId claim'den gelir; listeyle AYNI
/// görünürlük kuralı uygulanır (listede gizlenene Id ile ulaşılamasın).
/// </summary>
public sealed record GetEmployeeByIdQuery(int Id, int RequesterUserId) : IRequest<EmployeeDto?>;
