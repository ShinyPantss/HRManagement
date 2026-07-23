using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Interns;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin stajyer uçlarının sözleşmesi. Refit bu arayüzün implementasyonunu
/// çalışma anında kendisi üretir — elle HttpClient/JSON kodu yazmayız.
/// Attribute'lar HTTP metodunu ve yolu belirtir; {id} metot parametresine bağlanır.
/// </summary>
public interface IInternApi
{
    [Get("/api/interns")]
    Task<BaseResponse<List<InternResponse>>> GetAllAsync();

    [Get("/api/interns/{id}")]
    Task<BaseResponse<InternResponse>> GetByIdAsync(int id);

    [Post("/api/interns")]
    Task<BaseResponse<int?>> CreateAsync([Body] InternRequest request);

    [Put("/api/interns/{id}")]
    Task<BaseResponse<int?>> UpdateAsync(int id, [Body] InternRequest request);

    [Delete("/api/interns/{id}")]
    Task<BaseResponse<int?>> DeleteAsync(int id);
}
