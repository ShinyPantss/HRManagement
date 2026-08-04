using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Assistant;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Veri asistanı ucunun sözleşmesi. Rol kapısı API'de; buradaki
/// [Authorize] yalnızca kullanıcıyı boşuna bekletmemek içindir.
/// </summary>
public interface IAssistantApi
{
    [Post("/api/assistant/ask")]
    Task<BaseResponse<AssistantAnswerResponse>> AskAsync([Body] AskAssistantRequest request);
}
