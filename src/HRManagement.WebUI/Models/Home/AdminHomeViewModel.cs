using HRManagement.WebUI.Models.Api.AccountRequests;
using HRManagement.WebUI.Models.Api.Users;

namespace HRManagement.WebUI.Models.Home;

/// <summary>
/// Admin'in ana sayfası: SİSTEM panosu, İK panosu değil. Kadro/izin sayıları
/// yerine hesapların durumunu ve bekleyen işi gösterir.
///
/// Yeni bir API ucu YOK: var olan /api/users ve /api/accountrequests/pending
/// uçları birleştiriliyor (PersonalHomeViewModel ile aynı yaklaşım).
/// </summary>
public class AdminHomeViewModel
{
    public List<UserResponse> Users { get; set; } = [];
    public List<AccountRequestResponse> PendingRequests { get; set; } = [];

    public int TotalAccounts => Users.Count;
    public int ActiveAccounts => Users.Count(u => u.IsActive);
    public int PassiveAccounts => TotalAccounts - ActiveAccounts;
    public int PendingCount => PendingRequests.Count;

    public int ActiveAdminCount => Users.Count(u => u.Role == RoleDisplay.Admin && u.IsActive);

    /// <summary>
    /// Rol dağılımı — yalnızca AKTİF hesaplar. Pasif hesap "sistemde şu kadar
    /// yönetici var" iddiasını bozar, bu yüzden sayıma girmez.
    /// </summary>
    public IEnumerable<(string Label, int Count)> RoleBreakdown =>
        Users.Where(u => u.IsActive)
            .GroupBy(u => u.Role)
            .OrderBy(g => g.Key)
            .Select(g => (RoleDisplay.Label(g.Key), g.Count()));
}
