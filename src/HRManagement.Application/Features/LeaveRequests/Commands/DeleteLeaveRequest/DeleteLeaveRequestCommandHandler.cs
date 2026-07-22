using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandler : IRequestHandler<DeleteLeaveRequestCommand, Unit>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public DeleteLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    public async Task<Unit> Handle(DeleteLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        var actor = await _userRepository.GetByIdAsync(request.RequesterUserId);
        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        // Admin her talebi silebilir (yönetimsel temizlik).
        if (actor.Role != Role.Admin)
        {
            // Admin değilse: yalnızca KENDİ talebini VE yalnızca daha onaylanmamışsa.
            // Onaylı/İK aşamasındaki bir talebi silmek, kullanılan izni bakiyeden
            // düşüren kaydı yok ederek hakkı geri kazandırırdı (bakiye hilesi).
            var ownerUserId = await GetOwnerUserIdAsync(leaveRequest);

            if (ownerUserId is null || ownerUserId != actor.Id)
                throw new ValidationException("Yalnızca kendi izin talebinizi silebilirsiniz.");

            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new ValidationException(
                    "Yalnızca henüz onaylanmamış (beklemede) talepler silinebilir.");
        }

        await _leaveRequestRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }

    private async Task<int?> GetOwnerUserIdAsync(Domain.Entities.LeaveRequest leaveRequest)
    {
        if (leaveRequest.EmployeeId is int employeeId)
            return (await _employeeRepository.GetByIdAsync(employeeId))?.UserId;

        if (leaveRequest.InternId is int internId)
            return (await _internRepository.GetByIdAsync(internId))?.UserId;

        return null;
    }
}
