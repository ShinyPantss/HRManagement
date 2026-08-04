using System.Security.Claims;
using HRManagement.API.Models;
using HRManagement.API.Models.Assistant;
using HRManagement.Application.Features.Assistant.Queries.AskAssistant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssistantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Serbest SELECT çalıştığı için satır bazlı görünürlük kuralları devre dışı
    // kalır; bu yüzden yalnızca zaten tüm veriyi görebilen roller. Kapı burada
    // ve ayrıca handler'da: rol claim'i taşıyan bir token kaybolsa da Application
    // aktörü veritabanından tekrar doğrular.
    [Authorize(Roles = "HR,Admin")]
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskAssistantRequest request)
    {
        // ActorUserId gövdeden DEĞİL, token'dan okunur — istemci "ben Admin'im"
        // diyemesin diye. Soru dışındaki hiçbir şeye kullanıcı karar vermiyor.
        var result = await _mediator.Send(
            new AskAssistantQuery(request.Question, request.ConversationId, CurrentUserId()));

        var data = new AssistantAnswerResponse
        {
            Answer = result.Answer,
            ExecutedQueries = result.ExecutedQueries.ToList()
        };

        return Ok(BaseResponse<AssistantAnswerResponse>.Success(data));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
