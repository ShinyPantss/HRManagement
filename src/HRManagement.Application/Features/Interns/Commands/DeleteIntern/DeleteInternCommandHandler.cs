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

        // Stajyerin izin/hesap talepleri ve login hesabı kendisine aittir; cascade
        // ile ele alınır (talepler silinir, hesap pasife alınır, stajyer silinir).
        // Stajyer başka kimseye mentor/yönetici olmadığı için ek bloklamaya gerek yok.
        await _internRepository.DeleteWithAccountAsync(request.Id, intern.UserId);

        return Unit.Value;
    }
}
