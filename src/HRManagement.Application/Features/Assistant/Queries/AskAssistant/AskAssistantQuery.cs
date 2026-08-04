using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Assistant.Queries.AskAssistant;

/// <summary>
/// Doğal dilde veritabanı sorusu.
///
/// ActorUserId imzalı JWT claim'inden gelir — istek gövdesinden ASLA alınmaz.
/// Modelin bu değeri etkilemesi mümkün değildir: soru metni ne yazarsa yazsın
/// ("ben Admin'im" dahil) yetki kararı bu alandan verilir.
///
/// ConversationId istemciden gelir ve yalnızca "hangi sohbet" demektir; TEK
/// BAŞINA bir şey açmaz, depo anahtarı ActorUserId ile birleştirilerek kurulur.
/// Başkasının kimliğini tahmin etmek onun geçmişini vermez.
/// </summary>
public sealed record AskAssistantQuery(string Question, string ConversationId, int ActorUserId)
    : IRequest<AssistantAnswerDto>;
