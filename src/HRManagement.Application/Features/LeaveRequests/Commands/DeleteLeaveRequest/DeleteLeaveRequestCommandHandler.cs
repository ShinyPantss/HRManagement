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

    // İptal / geri çekme kuralları (kullanıcı kararı):
    //   izin BAŞLADIYSA (bugün ≥ başlangıç)     → hiçbir şey yapılamaz
    //   onaylanmadıysa + başlamadıysa           → DİREKT İPTAL: kayıt silinir
    //   onaylandıysa  + başlamadıysa            → GERİ ÇEKME: kayıt SİLİNMEZ,
    //                                             Cancelled'a çekilir (denetim izi
    //                                             kalır, günler bakiyeye döner)
    //   reddedildi / zaten geri çekildi         → işlem yok (akış sonuçlanmış)
    // Admin istisnası: her talebi silebilir (yönetimsel temizlik / veri onarımı).
    public async Task<Unit> Handle(DeleteLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.Id);

        if (leaveRequest is null)
            throw new ValidationException("İzin talebi bulunamadı.");

        var actor = await _userRepository.GetByIdAsync(request.RequesterUserId);
        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        if (actor.Role == Role.Admin)
        {
            await _leaveRequestRepository.DeleteAsync(request.Id);
            return Unit.Value;
        }

        var ownerUserId = await GetOwnerUserIdAsync(leaveRequest);

        if (ownerUserId is null || ownerUserId != actor.Id)
            throw new ValidationException("Yalnızca kendi izin talebinizi iptal edebilirsiniz.");

        // "Bugün" tanımı sistemin geri kalanıyla aynı kaynaktan (UTC). Başlangıç
        // gününün sabahı da "izne girilmiş" sayılır — o günün yerine başkası
        // planlanamayacağı için geri almanın operasyonel karşılığı kalmamıştır.
        if (leaveRequest.StartDate.Date <= DateTime.UtcNow.Date)
            throw new ValidationException("İzin başlamış; artık iptal edilemez veya geri çekilemez.");

        switch (leaveRequest.Status)
        {
            // Henüz kimse onaylamadı: rezerve edilen hak dışında iz yok, kayıt silinir.
            case LeaveStatus.Pending:
            case LeaveStatus.PendingHr:
                await _leaveRequestRepository.DeleteAsync(request.Id);
                break;

            // Onaylı talep geri çekilir ama SİLİNMEZ: onay izi (kim, ne zaman)
            // denetim için kalmalı. Cancelled, kullanılan-gün ve çakışma
            // sorgularının statü listelerinde olmadığından günler kendiliğinden
            // bakiyeye döner.
            case LeaveStatus.Approved:
                leaveRequest.Status = LeaveStatus.Cancelled;
                await _leaveRequestRepository.UpdateAsync(leaveRequest);
                break;

            case LeaveStatus.Cancelled:
                throw new ValidationException("Bu talep zaten geri çekilmiş.");

            default: // Rejected
                throw new ValidationException("Reddedilmiş talep iptal edilemez; kayıt geçmişte kalır.");
        }

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
