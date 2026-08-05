using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Units.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

public sealed class CreateInternCommandHandler : IRequestHandler<CreateInternCommand, int>
{
    private readonly IInternRepository _internRepository;
    private readonly IAccountRequestRepository _accountRequestRepository;
    private readonly IUnitRepository _unitRepository;

    public CreateInternCommandHandler(
        IInternRepository internRepository,
        IAccountRequestRepository accountRequestRepository,
        IUnitRepository unitRepository)
    {
        _internRepository = internRepository;
        _accountRequestRepository = accountRequestRepository;
        _unitRepository = unitRepository;
    }

    // Input validation CreateInternCommandValidator'da; buraya gelen mesaj geçerlidir.
    public async Task<int> Handle(CreateInternCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        // Seçilen birim (varsa) bu departmana ait olmalı.
        await UnitAssignment.EnsureUnitInDepartmentAsync(_unitRepository, request.UnitId, request.DepartmentId);

        // E-posta benzersizliği: çalışan tarafındaki kuralın aynısı. DB'de UNIQUE
        // kısıt var, ama ona takılmak 500 üretir; kuralı burada önden uygulayıp
        // anlaşılır bir 400 mesajı veriyoruz.
        if (await _internRepository.GetByEmailAsync(email) is not null)
            throw new ValidationException("Bu e-posta ile kayıtlı bir stajyer zaten var.");

        var intern = new Intern
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            University = request.University.Trim(),
            Major = request.Major?.Trim() ?? string.Empty,
            Grade = request.Grade,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MentorId = request.MentorId,
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId
        };

        var internId = await _internRepository.AddAsync(intern);

        // Otomatik hesap talebi (çalışanla simetrik): stajyer eklenince Admin'e
        // Pending talep düşer, onay Admin'de kalır. Stajyer kaydı oluşturulurken
        // zaten bir hesaba bağlanmadığı için ek guard yok; yalnızca kutu belirler.
        // Önerilen rol Intern — Admin onay ekranında değiştirebilir.
        if (request.RequestLoginAccount)
            await _accountRequestRepository.AddAsync(new AccountRequest
            {
                InternId = internId,
                RequestedByUserId = request.CreatedByUserId,
                SuggestedRole = Role.Intern,
                Note = "Stajyer kaydı oluşturulurken otomatik açıldı.",
                Status = AccountRequestStatus.Pending
            });

        return internId;
    }
}
