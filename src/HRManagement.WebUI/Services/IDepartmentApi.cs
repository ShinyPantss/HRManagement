using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Departments;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin departman uçlarının sözleşmesi. Refit bu arayüzün implementasyonunu
/// çalışma anında kendisi üretir — elle HttpClient/JSON kodu yazmayız.
/// Attribute'lar HTTP metodunu ve yolu belirtir; {id} metot parametresine bağlanır.
/// </summary>
public interface IDepartmentApi
{
    [Get("/api/departments")]
    Task<BaseResponse<List<DepartmentResponse>>> GetAllAsync();

    [Get("/api/departments/{id}")]
    Task<BaseResponse<DepartmentResponse>> GetByIdAsync(int id);

    [Post("/api/departments")]
    Task<BaseResponse<int>> CreateAsync([Body] DepartmentRequest request);

    [Put("/api/departments/{id}")]
    Task<BaseResponse<int>> UpdateAsync(int id, [Body] DepartmentRequest request);

    [Delete("/api/departments/{id}")]
    Task<BaseResponse<int>> DeleteAsync(int id);
}
