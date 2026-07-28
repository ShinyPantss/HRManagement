using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Dashboard;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin pano uçlarının sözleşmesi. Refit implementasyonu çalışma anında üretir.
/// İK/Admin ana sayfa panosu (rol kapısı API'de).
/// </summary>
public interface IDashboardApi
{
    [Get("/api/dashboard/hr")]
    Task<BaseResponse<HrDashboardResponse>> GetHrDashboardAsync();
}
