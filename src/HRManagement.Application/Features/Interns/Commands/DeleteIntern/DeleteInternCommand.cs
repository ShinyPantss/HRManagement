using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.DeleteIntern;

public sealed class DeleteInternCommand : IRequest<Unit>
{
    public DeleteInternCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
