using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    int Id,
    string FirstName,
    string LastName,
    string? NationalId,
    string Email,
    string? Phone,
    DateTime BirthDate,
    DateTime HireDate,
    int DepartmentId,
    int? UnitId,
    int? UserId,
    int? ManagerId,
    SeniorityLevel? Seniority,
    int? AnnualLeaveDays,
    bool IsActive) : IRequest<Unit>;
