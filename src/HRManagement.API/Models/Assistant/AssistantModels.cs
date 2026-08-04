namespace HRManagement.API.Models.Assistant;

/// <summary>
/// Kullanıcının doğal dildeki sorusu. Tek alan — asistanın sözleşmesi
/// bilinçli olarak dar: "ne sorduğunu" yaz, gerisini o çözsün.
/// </summary>
public class AskAssistantRequest
{
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Hangi sohbete ait olduğu. İstemci üretir (UUID), geçmiş bununla
    /// bulunur. Tek başına yetki taşımaz — depo anahtarı token'daki
    /// kullanıcı kimliğiyle birleştirilerek kurulur.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
}

public class AssistantAnswerResponse
{
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Cevabı üretmek için gerçekten çalıştırılan SELECT'ler. Şeffaflık için
    /// döndürülür: kullanıcı sayının nereden geldiğini görebilsin, modele
    /// körlemesine güvenmek zorunda kalmasın.
    /// </summary>
    public List<string> ExecutedQueries { get; set; } = [];
}
