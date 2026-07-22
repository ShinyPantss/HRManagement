using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;

public sealed class GetLeaveRequestsByEmployeeQueryHandler : IRequestHandler<GetLeaveRequestsByEmployeeQuery, IEnumerable<LeaveRequestDto>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetLeaveRequestsByEmployeeQueryHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<LeaveRequestDto>> Handle(GetLeaveRequestsByEmployeeQuery request, CancellationToken cancellationToken)
    {
        await EnsureCanViewAsync(request.EmployeeId, request.RequesterUserId);

        var leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(request.EmployeeId);
        return leaveRequests.Select(LeaveRequestMapping.ToDto);
    }

    // İzin geçmişi hassastır; kimin görebileceği burada denetlenir.
    private async Task EnsureCanViewAsync(int targetEmployeeId, int requesterUserId)
    {
        var actor = await _userRepository.GetByIdAsync(requesterUserId);
        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        // HR ve Admin herkesin geçmişini görebilir (İK yönetimi).
        if (actor.Role is Role.HR or Role.Admin)
            return;

        var actorEmployee = await _employeeRepository.GetByUserIdAsync(actor.Id);
        if (actorEmployee is null)
            throw new ValidationException("Bu izin kayıtlarını görüntüleme yetkiniz yok.");

        // Kişinin kendisi, ya da hedefin yönetici zincirinde yukarıdaki biri.
        var canView = actorEmployee.Id == targetEmployeeId
            || await _employeeRepository.IsInManagerChainAsync(actorEmployee.Id, targetEmployeeId);

        if (!canView)
            throw new ValidationException("Yalnızca kendinizin veya ekibinizin izin kayıtlarını görebilirsiniz.");
    }
}
