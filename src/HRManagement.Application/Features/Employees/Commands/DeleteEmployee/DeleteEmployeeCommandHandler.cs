using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IInternRepository _internRepository;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IInternRepository internRepository)
    {
        _employeeRepository = employeeRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _internRepository = internRepository;
    }

    public async Task<Unit> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.Id);

        if (employee is null)
            throw new ValidationException("Çalışan bulunamadı.");

        // İzin talepleri GEÇMİŞ/denetimdir; silmek İK kaydını yok eder.
        // Böyle bir çalışan silinmez, PASİFE alınır (IsActive = false).
        if (await _leaveRequestRepository.ExistsByEmployeeIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışana ait izin talepleri var. Kaydı silmek yerine çalışanı pasife alın.");

        // Mentor/ekip: bunlar BAŞKA kişiler; onları silmeyiz. Önce ilişki çözülmeli.
        if (await _internRepository.ExistsByMentorIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışan bir veya daha fazla stajyerin mentoru. Önce stajyerlere yeni mentor atayın.");

        if (await _employeeRepository.ExistsByManagerIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışana bağlı ekip üyeleri var. Önce onları başka bir yöneticiye bağlayın.");

        // Hesap talepleri (denetim) ve login hesabı çalışana AİTTİR: cascade ile
        // ele alınır — talepler silinir, hesap pasife alınır, çalışan silinir;
        // hepsi tek transaction'da.
        await _employeeRepository.DeleteWithAccountAsync(request.Id, employee.UserId);

        return Unit.Value;
    }
}
