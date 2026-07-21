using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// "Yeni çalışan ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string? NationalId,
    string Email,
    string? Phone,
    DateTime BirthDate,
    DateTime HireDate,
    string Position,
    int DepartmentId) : IRequest<int>;
