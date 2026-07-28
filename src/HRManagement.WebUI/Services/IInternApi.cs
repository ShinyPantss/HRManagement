using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Interns;
using HRManagement.WebUI.Models.Api.Mentorship;
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

    // "Profilim" (stajyerin kendisi): kimlik token'dan çözülür, id yok.
    [Get("/api/interns/me")]
    Task<BaseResponse<MyInternProfileResponse>> GetMyProfileAsync();

    // "Görevlerim" (stajyerin kendisi): kimlik token'dan çözülür, id yok.
    [Get("/api/interns/my-tasks")]
    Task<BaseResponse<MyInternTasksResponse>> GetMyTasksAsync();

    [Put("/api/interns/my-tasks/{taskId}/status")]
    Task<BaseResponse<int?>> UpdateMyTaskStatusAsync(int taskId, [Body] UpdateInternTaskStatusRequest request);
}
