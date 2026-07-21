using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

public sealed class CreateInternCommandHandler : IRequestHandler<CreateInternCommand, int>
{
    private readonly IInternRepository _internRepository;

    public CreateInternCommandHandler(IInternRepository internRepository)
    {
        _internRepository = internRepository;
    }

    // Input validation CreateInternCommandValidator'da; buraya gelen mesaj geçerlidir.
    public async Task<int> Handle(CreateInternCommand request, CancellationToken cancellationToken)
    {
        var intern = new Intern
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            University = request.University.Trim(),
            Major = request.Major?.Trim() ?? string.Empty,
            Grade = request.Grade,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MentorId = request.MentorId,
            DepartmentId = request.DepartmentId
        };

        return await _internRepository.AddAsync(intern);
    }
}
