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
    bool IsActive) : IRequest<Unit>;
