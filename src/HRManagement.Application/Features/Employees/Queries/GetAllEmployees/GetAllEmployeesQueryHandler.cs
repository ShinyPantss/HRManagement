using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using HRManagement.Application.Services;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeDto>>
{
    private readonly EmployeeVisibility _visibility;
    private readonly IUserRepository _userRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetAllEmployeesQueryHandler(
        EmployeeVisibility visibility,
        IUserRepository userRepository,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _visibility = visibility;
        _userRepository = userRepository;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
    {
        // Filtreleme sorgunun İÇİNDE: yetkisiz kayıt bu katmandan yukarı hiç çıkmaz.
        var employees = (await _visibility.GetVisibleAsync(request.RequesterUserId)).ToList();

        // İzin bakiyesi HASSAS veridir; yalnızca İK/Admin listesinde doldurulur.
        // "Ekibim" görünümünde bir çalışan iş arkadaşlarının bakiyesini görmemeli.
        // EmployeeDetailAssembler'daki CanSeeLeave kırpmasıyla aynı ilke: göremeyecek
        // olana veri GÖNDERİLMEZ, sadece ekranda gizlenmez — aksi hâlde JSON'da durur
        // ve tarayıcı konsolundan okunabilirdi.
        var actor = await _userRepository.GetByIdAsync(request.RequesterUserId);
        var canSeeLeave = actor is not null && actor.Role is Role.HR or Role.Admin;

        // Göremeyecekse sorgu HİÇ çalışmaz — hem gizlilik hem boşuna iş yapmamak.
        // Kullanılan günler tek GROUP BY ile gelir: kişi başına sorgu (N+1) yok.
        var usedByEmployee = canSeeLeave
            ? await _leaveRequestRepository.GetUsedAnnualDaysByEmployeeAsync()
            : null;

        // "Bugün" tek kaynaktan: izin hesaplarının tamamı UTC tarihe göre çalışır.
        var today = DateTime.UtcNow.Date;

        return employees
            .Select(employee =>
            {
                var dto = EmployeeMapping.ToDto(employee);

                if (usedByEmployee is not null)
                {
                    // Hak ediş saf bir hesap, veritabanına gitmez. Hiç yıllık izin
                    // talebi olmayan çalışan sözlükte yoktur → 0 sayılır.
                    dto.AccruedLeaveDays = LeaveEntitlement.AccruedEntitlement(
                        employee.HireDate, today, employee.AnnualLeaveDays);

                    dto.UsedLeaveDays = usedByEmployee.TryGetValue(employee.Id, out var used) ? used : 0;
                    dto.RemainingLeaveDays = dto.AccruedLeaveDays - dto.UsedLeaveDays;
                }

                return dto;
            })
            .ToList();
    }
}
