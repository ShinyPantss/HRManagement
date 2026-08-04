namespace HRManagement.WebUI.Models.Api.Assistant;

/// <summary>
/// API'deki AskAssistantRequest'in WebUI kopyası. Paylaşılan Contracts projesi
/// olmadığı için JSON şekli iki tarafta elle senkron tutulur.
/// </summary>
public class AskAssistantRequest
{
    public string Question { get; set; } = string.Empty;

    /// <summary>Hangi sohbete ait olduğu — panel üretir, geçmiş bununla bulunur.</summary>
    public string ConversationId { get; set; } = string.Empty;
}

public class AssistantAnswerResponse
{
    public string Answer { get; set; } = string.Empty;

    /// <summary>Cevabı üretmek için gerçekten çalıştırılan SELECT'ler.</summary>
    public List<string> ExecutedQueries { get; set; } = [];
}
