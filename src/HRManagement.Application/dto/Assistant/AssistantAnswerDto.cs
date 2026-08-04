namespace HRManagement.Application.DTOs;

/// <summary>
/// Asistanın cevabı.
///
/// <see cref="ExecutedQueries"/> bilinçli olarak dışarı veriliyor: kullanıcı
/// cevabın hangi sorgudan geldiğini görebilmeli. Yapay zekâ üretimi bir cevabın
/// denetlenebilir olması, doğru çıkmasından daha önemlidir — yanlışsa nereden
/// geldiği anlaşılsın.
/// </summary>
public class AssistantAnswerDto
{
    /// <summary>Modelin Türkçe cevabı.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>Bu cevap için gerçekten çalıştırılan SELECT sorguları (sırasıyla).</summary>
    public List<string> ExecutedQueries { get; set; } = [];
}
