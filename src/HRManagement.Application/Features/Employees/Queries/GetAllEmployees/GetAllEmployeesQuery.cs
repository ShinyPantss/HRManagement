using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetAllEmployees;

/// <summary>
/// Çalışan listesi. RequesterUserId imzalı JWT claim'inden gelir; liste
/// isteği yapanın görebilecekleriyle SINIRLIDIR (Manager yalnızca ekibini görür).
/// </summary>
public sealed record GetAllEmployeesQuery(int RequesterUserId) : IRequest<IEnumerable<EmployeeDto>>;
