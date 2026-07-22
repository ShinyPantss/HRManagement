using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface IAccountRequestRepository
{
    Task<AccountRequest?> GetByIdAsync(int id);
    Task<int> AddAsync(AccountRequest request);
    Task UpdateAsync(AccountRequest request);

    /// <summary>Bir kişinin şu an BEKLEYEN talebi var mı? (mükerrer talebi önler)</summary>
    Task<bool> HasPendingAsync(int? employeeId, int? internId);

    /// <summary>
    /// Bu kişiye ait HERHANGİ bir talep (durum fark etmeksizin) var mı?
    /// Silme öncesi bağımlılık kontrolü: talep FK'sine takılıp 500 dönmesin,
    /// üstelik denetim izi (kim talep etti/işledi) kaybolmasın.
    /// </summary>
    Task<bool> ExistsForEmployeeAsync(int employeeId);
    Task<bool> ExistsForInternAsync(int internId);

    /// <summary>
    /// Bekleyen talepler + kişi/talep-eden adları (JOIN). Bekleyen talepler
    /// ekranı için; çıplak id yerine okunabilir DTO döner.
    /// </summary>
    Task<IEnumerable<AccountRequestDto>> GetPendingWithNamesAsync();
}
