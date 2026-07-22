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

        // İzin talepleri geçmiş kayıttır; çalışanla birlikte silinmesi veri kaybıdır.
        // Doğru yaklaşım kaydı silmek değil, çalışanı pasife almaktır (IsActive = false).
        if (await _leaveRequestRepository.ExistsByEmployeeIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışana ait izin talepleri var. Kaydı silmek yerine çalışanı pasife alın.");

        if (await _internRepository.ExistsByMentorIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışan bir veya daha fazla stajyerin mentoru. Önce stajyerlere yeni mentor atayın.");

        // ManagerId self-FK'sı: astı olan çalışan silinirse FK ihlali → 500 olurdu.
        if (await _employeeRepository.ExistsByManagerIdAsync(request.Id))
            throw new ValidationException(
                "Bu çalışana bağlı ekip üyeleri var. Önce onları başka bir yöneticiye bağlayın.");

        await _employeeRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }
}
