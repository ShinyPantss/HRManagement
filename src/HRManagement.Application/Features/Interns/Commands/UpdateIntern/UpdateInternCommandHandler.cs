using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateIntern;

public sealed class UpdateInternCommandHandler : IRequestHandler<UpdateInternCommand, Unit>
{
    private readonly IInternRepository _internRepository;

    public UpdateInternCommandHandler(IInternRepository internRepository)
    {
        _internRepository = internRepository;
    }

    // Input validation UpdateInternCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALI kalır.
    public async Task<Unit> Handle(UpdateInternCommand request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByIdAsync(request.Id);

        if (intern is null)
            throw new ValidationException("Stajyer bulunamadı.");

        intern.FirstName = request.FirstName.Trim();
        intern.LastName = request.LastName.Trim();
        intern.Email = request.Email.Trim();
        intern.University = request.University.Trim();
        intern.Major = request.Major?.Trim() ?? string.Empty;
        intern.Grade = request.Grade;
        intern.StartDate = request.StartDate;
        intern.EndDate = request.EndDate;
        intern.MentorId = request.MentorId;
        intern.DepartmentId = request.DepartmentId;

        await _internRepository.UpdateAsync(intern);

        return Unit.Value;
    }
}
