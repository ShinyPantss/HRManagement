using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.DeleteIntern;

public sealed class DeleteInternCommandHandler : IRequestHandler<DeleteInternCommand, Unit>
{
    private readonly IInternRepository _internRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IAccountRequestRepository _accountRequestRepository;

    public DeleteInternCommandHandler(
        IInternRepository internRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IAccountRequestRepository accountRequestRepository)
    {
        _internRepository = internRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _accountRequestRepository = accountRequestRepository;
    }

    public async Task<Unit> Handle(DeleteInternCommand request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByIdAsync(request.Id);

        if (intern is null)
            throw new ValidationException("Stajyer bulunamadı.");

        // Bağımlılık kontrolleri: FK'ye takılıp 500 dönmesin, geçmiş/denetim kaybolmasın.
        if (await _leaveRequestRepository.ExistsByInternIdAsync(request.Id))
            throw new ValidationException(
                "Bu stajyere ait izin talepleri var. Kaydı silmek yerine ilişkiyi koruyun.");

        if (await _accountRequestRepository.ExistsForInternAsync(request.Id))
            throw new ValidationException(
                "Bu stajyere ait hesap talepleri var. Kaydı silmek yerine pasife alın.");

        await _internRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }
}
