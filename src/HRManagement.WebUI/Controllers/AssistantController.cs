using HRManagement.WebUI.Models.Api.Assistant;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Sohbet panelinin tek ucu. Sayfa döndürmez; panel fetch ile çağırır,
/// JSON alır. İş yapmaz — Refit üzerinden API'ye iletir.
///
/// Panel neden doğrudan API'ye gitmiyor: JWT cookie ticket'ının içinde,
/// tarayıcıya hiç verilmiyor. İstek sunucudan geçmek zorunda ki
/// BearerTokenHandler token'ı ekleyebilsin.
/// </summary>
[Authorize(Roles = "HR,Admin")]
public class AssistantController : Controller
{
    private readonly IAssistantApi _assistantApi;

    public AssistantController(IAssistantApi assistantApi)
    {
        _assistantApi = assistantApi;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromForm] string question, [FromForm] string conversationId)
    {
        if (string.IsNullOrWhiteSpace(question))
            return Json(new { ok = false, error = "Soru boş olamaz." });

        var response = await _assistantApi.AskAsync(
            new AskAssistantRequest { Question = question, ConversationId = conversationId });

        // API hata gövdesi de BaseResponse olduğu için exception değil,
        // IsSuccess=false okunur (RefitSettings.ExceptionFactory kapalı).
        if (!response.IsSuccess || response.Data is null)
            return Json(new { ok = false, error = response.Message ?? "Asistana ulaşılamadı." });

        return Json(new
        {
            ok = true,
            answer = response.Data.Answer,
            queries = response.Data.ExecutedQueries
        });
    }
}
