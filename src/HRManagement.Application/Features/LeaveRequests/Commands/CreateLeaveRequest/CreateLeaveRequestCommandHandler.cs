using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Services;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, int>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public CreateLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    // Input validation (tarih sırası, tür) CreateLeaveRequestCommandValidator'da.
    // Buradaki her kural veritabanına bakar; sırası bilinçlidir:
    //   kimlik çözümü → aktiflik → tarih çakışması → yıllık izin hakkı.
    public async Task<int> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // 1) Talep sahibi kim? Hesap önce çalışan, yoksa stajyer kaydına çözülür.
        var employee = await _employeeRepository.GetByUserIdAsync(request.RequesterUserId);
        Intern? intern = employee is null
            ? await _internRepository.GetByUserIdAsync(request.RequesterUserId)
            : null;

        if (employee is null && intern is null)
            throw new ValidationException(
                "Hesabınız bir çalışan veya stajyer kaydına bağlı değil. İK ile iletişime geçin.");

        if (employee is not null && !employee.IsActive)
            throw new ValidationException("Pasif çalışan kaydı için izin talebi oluşturulamaz.");

        // Stajyerde IsActive sütunu yok; aktiflik staj süresinden okunur.
        // Süresi dolmuş staj için talep açılmamalı.
        if (intern is not null && intern.EndDate.Date < DateTime.UtcNow.Date)
            throw new ValidationException("Staj süresi sona ermiş; izin talebi oluşturulamaz.");

        // 2) Tarih çakışması: aynı kişinin aktif (bekleyen/onaylı) talebiyle kesişme.
        if (await _leaveRequestRepository.HasOverlapAsync(
                employee?.Id, intern?.Id, request.StartDate, request.EndDate))
            throw new ValidationException("Bu tarih aralığı mevcut bir izin talebinizle çakışıyor.");

        // 3) Yıllık izin hak kontrolü — yalnızca Annual için. Unpaid/Sick haktan
        //    düşmez, bu yüzden kontrolsüz geçer (1 yılını doldurmamış çalışanın
        //    izin yolu budur).
        if (request.Type == LeaveType.Annual)
        {
            if (intern is not null)
                throw new ValidationException(
                    "Stajyerler yıllık ücretli izin hakkı biriktirmez. Ücretsiz izin talep edebilirsiniz.");

            await EnsureAnnualBalanceAsync(employee!, request.StartDate, request.EndDate);
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee?.Id,
            InternId = intern?.Id,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            Status = LeaveStatus.Pending
        };

        return await _leaveRequestRepository.AddAsync(leaveRequest);
    }

    // Kümülatif bakiye kuralı (kullanıcı kararı): kullanılan + talep ≤ hak edilen +
    // bir sonraki yılın hakkı (avans sınırı). "Kullanılan"a BEKLEYEN talepler de
    // dahildir — her talep yerini baştan rezerve eder; yoksa dört bekleyen talep
    // ayrı ayrı kontrolü geçip hakkı katlardı. Borç, kümülatif olduğu için yeni
    // yıla kendiliğinden devreder.
    private async Task EnsureAnnualBalanceAsync(Employee employee, DateTime startDate, DateTime endDate)
    {
        var today = DateTime.UtcNow.Date;

        var accrued = LeaveEntitlement.AccruedEntitlement(employee.HireDate, today, employee.AnnualLeaveDays);
        var nextGrant = LeaveEntitlement.NextGrant(employee.HireDate, today, employee.AnnualLeaveDays);

        var used = await _leaveRequestRepository.GetTotalUsedAnnualDaysAsync(employee.Id);
        var requested = LeaveEntitlement.TotalDays(startDate, endDate);

        if (used + requested > accrued + nextGrant)
            throw new ValidationException(
                $"Yıllık izin hakkı aşılıyor. Bakiye {accrued - used} gün " +
                $"(hak edilen {accrued}, kullanılan+bekleyen {used}); talep {requested} gün, " +
                $"avans sınırı {nextGrant} gün.");
    }
}
