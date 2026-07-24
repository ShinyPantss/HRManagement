using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Interns;
using HRManagement.WebUI.Models.Api.Mentorship;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Mentorluk uçlarının sözleşmesi. Bu uçlarda rol kısıtı yoktur — API,
/// "istekçi bu stajyerin mentoru mu?" ilişkisine bakar (MentorshipGuard).
/// </summary>
public interface IMentorshipApi
{
    [Get("/api/interns/mentored")]
    Task<BaseResponse<List<InternResponse>>> GetMyInternsAsync();

    [Get("/api/interns/{id}/mentorship")]
    Task<BaseResponse<MentoredInternDetailResponse>> GetDetailAsync(int id);

    [Post("/api/interns/{id}/tasks")]
    Task<BaseResponse<int?>> AddTaskAsync(int id, [Body] AddInternTaskRequest request);

    [Post("/api/interns/{id}/notes")]
    Task<BaseResponse<int?>> AddNoteAsync(int id, [Body] AddInternNoteRequest request);

    [Put("/api/interns/tasks/{taskId}/status")]
    Task<BaseResponse<int?>> UpdateTaskStatusAsync(int taskId, [Body] UpdateInternTaskStatusRequest request);
}
