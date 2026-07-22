using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// "Yeni çalışan ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
///
/// UserId    → giriş hesabıyla ilişkilendirme (5.2: sonradan da bağlanabilir, null olabilir)
/// ManagerId → bağlı olduğu yönetici; izin onay zinciri buradan kurulur
/// AnnualLeaveDays → izin hakkını elle ezme; normalde null (kıdemden hesaplanır)
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
    int DepartmentId,
    int? UserId,
    int? ManagerId,
    int? AnnualLeaveDays) : IRequest<int>;
