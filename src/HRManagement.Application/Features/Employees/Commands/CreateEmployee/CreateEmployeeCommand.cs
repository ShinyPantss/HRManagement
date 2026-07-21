using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// "Yeni çalışan ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed class CreateEmployeeCommand : IRequest<int>
{
    public CreateEmployeeCommand(
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        string position,
        int departmentId)
    {
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        Position = position;
        DepartmentId = departmentId;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public string Position { get; }
    public int DepartmentId { get; }
}
