using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommand : IRequest<Unit>
{
    public DeleteEmployeeCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
