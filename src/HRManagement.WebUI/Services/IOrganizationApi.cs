using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Organization;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Organizasyon şeması ucunun sözleşmesi. Tek çağrı departman, birim ve kadro
/// üyeliğini birden getirir — /api/employees'in aksine role göre daralmaz,
/// çünkü yalnızca rehber düzeyinde alan taşır (bkz. API/OrganizationController).
/// </summary>
public interface IOrganizationApi
{
    [Get("/api/organization")]
    Task<BaseResponse<OrganizationResponse>> GetAsync();
}
