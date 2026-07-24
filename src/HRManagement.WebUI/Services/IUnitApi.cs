using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Units;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin birim ucunun sözleşmesi. Formlardaki departmana-göre-süzülen birim
/// dropdown'ını doldurmak için tüm birimler tek seferde çekilir.
/// </summary>
public interface IUnitApi
{
    [Get("/api/units")]
    Task<BaseResponse<List<UnitResponse>>> GetAllAsync();
}
