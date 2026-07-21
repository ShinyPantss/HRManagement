using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.DeleteIntern;

public sealed class DeleteInternCommandHandler : IRequestHandler<DeleteInternCommand, Unit>
{
    private readonly IInternRepository _internRepository;

    public DeleteInternCommandHandler(IInternRepository internRepository)
    {
        _internRepository = internRepository;
    }

    public async Task<Unit> Handle(DeleteInternCommand request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByIdAsync(request.Id);

        if (intern is null)
            throw new ValidationException("Stajyer bulunamadı.");

        await _internRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }
}
