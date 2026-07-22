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
    string Position,
    int DepartmentId,
    int? UserId,
    int? ManagerId,
    int? AnnualLeaveDays,
    bool IsActive) : IRequest<Unit>;
