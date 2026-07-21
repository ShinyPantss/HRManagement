using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>
/// "Yeni departman ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed class CreateDepartmentCommand : IRequest<int>
{
    public CreateDepartmentCommand(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }
}
