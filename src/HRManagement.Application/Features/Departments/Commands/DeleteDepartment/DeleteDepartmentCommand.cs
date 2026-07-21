using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommand : IRequest<Unit>
{
    public DeleteDepartmentCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
