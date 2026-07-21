using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommand : IRequest<Unit>
{
    public UpdateDepartmentCommand(int id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public int Id { get; }
    public string Name { get; }
    public string? Description { get; }
}
