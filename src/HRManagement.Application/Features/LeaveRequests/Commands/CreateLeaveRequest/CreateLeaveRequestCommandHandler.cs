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

    // Kural (kullanıcı kararı): kullanılan + talep ≤ kazanılmış hak + avans limiti.
    // "Kullanılan"a BEKLEYEN talepler de dahildir — her talep yerini baştan rezerve
    // eder; yoksa dört bekleyen talep ayrı ayrı kontrolü geçip hakkı katlardı.
    private async Task EnsureAnnualBalanceAsync(Employee employee, DateTime startDate, DateTime endDate)
    {
        var today = DateTime.UtcNow.Date;

        var entitled = LeaveEntitlement.EntitledDays(employee.HireDate, today, employee.AnnualLeaveDays);
        var advance = LeaveEntitlement.AdvanceLimit(employee.HireDate, today);
        var (periodStart, periodEnd) = LeaveEntitlement.CurrentPeriod(employee.HireDate, today);

        var used = await _leaveRequestRepository.GetUsedAnnualDaysAsync(employee.Id, periodStart, periodEnd);
        var requested = LeaveEntitlement.TotalDays(startDate, endDate);

        if (used + requested > entitled + advance)
            throw new ValidationException(
                $"Yıllık izin hakkı aşılıyor: bu dönemde kullanılan+bekleyen {used} gün, " +
                $"talep {requested} gün; hak {entitled} gün + avans sınırı {advance} gün.");
    }
}
