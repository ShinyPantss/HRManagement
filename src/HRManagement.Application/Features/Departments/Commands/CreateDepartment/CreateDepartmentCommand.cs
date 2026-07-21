using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>
/// "Yeni departman ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed record CreateDepartmentCommand(string Name, string? Description) : IRequest<int>;
