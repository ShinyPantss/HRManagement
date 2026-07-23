using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Employees;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin çalışan uçlarının sözleşmesi. Refit bu arayüzün implementasyonunu
/// çalışma anında kendisi üretir — elle HttpClient/JSON kodu yazmayız.
/// Attribute'lar HTTP metodunu ve yolu belirtir; {id} metot parametresine bağlanır.
/// </summary>
public interface IEmployeeApi
{
    [Get("/api/employees")]
    Task<BaseResponse<List<EmployeeResponse>>> GetAllAsync();

    [Get("/api/employees/{id}")]
    Task<BaseResponse<EmployeeResponse>> GetByIdAsync(int id);

    [Post("/api/employees")]
    Task<BaseResponse<int?>> CreateAsync([Body] CreateEmployeeRequest request);

    [Put("/api/employees/{id}")]
    Task<BaseResponse<int?>> UpdateAsync(int id, [Body] UpdateEmployeeRequest request);

    [Delete("/api/employees/{id}")]
    Task<BaseResponse<int?>> DeleteAsync(int id);
}
