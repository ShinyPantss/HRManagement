using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommand : IRequest<Unit>
{
    public UpdateEmployeeCommand(
        int id,
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        string position,
        int departmentId,
        bool isActive)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        Position = position;
        DepartmentId = departmentId;
        IsActive = isActive;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public string Position { get; }
    public int DepartmentId { get; }
    public bool IsActive { get; }
}
