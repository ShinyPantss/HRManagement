using System.ComponentModel.DataAnnotations;
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

    public async Task<int> Handle(CreateInternCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ValidationException("Ad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("Soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("E-posta zorunludur.");

        if (string.IsNullOrWhiteSpace(request.University))
            throw new ValidationException("Üniversite zorunludur.");

        if (request.StartDate.Date > request.EndDate.Date)
            throw new ValidationException("Staj başlangıç tarihi bitiş tarihinden sonra olamaz.");

        if (request.Grade < 1 || request.Grade > 8)
            throw new ValidationException("Sınıf 1-8 arasında olmalıdır.");

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
