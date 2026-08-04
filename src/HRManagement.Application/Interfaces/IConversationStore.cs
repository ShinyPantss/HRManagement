namespace HRManagement.Application.Interfaces;

/// <summary>Tamamlanmış bir soru-cevap turu.</summary>
public sealed record ConversationTurn(string Question, string Answer);

/// <summary>
/// Pencereye sığan turlar + pencereden DÜŞEN tur sayısı.
///
/// Düşen sayısı bilinçli olarak taşınıyor: model bağlamın bir kısmının
/// eksik olduğunu bilmezse, eski bir kısıtı (ör. "sadece aktif çalışanlar")
/// unuttuğunu fark etmeden kendinden emin YANLIŞ cevap verir. Sayıyı
/// bilirse varsayım yerine soru sorabilir.
/// </summary>
public sealed record ConversationHistory(
    IReadOnlyList<ConversationTurn> Turns,
    int DroppedTurnCount);

/// <summary>
/// Sohbet geçmişi deposu. Kalıcı DEĞİLDİR — süreç belleğinde yaşar,
/// uygulama yeniden başlayınca sıfırlanır. Sohbet geçmişi veri değil
/// kolaylıktır; kaybolursa kullanıcı sorusunu tekrar sorar.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Modele taşınacak en fazla tur. Asıl freni bu değil
    /// <see cref="MaxAnswerChars"/> yapar — tek bir tablo cevabı
    /// birkaç kısa turdan fazla yer kaplayabiliyor.
    /// </summary>
    const int MaxTurns = 12;

    /// <summary>Bir cevabın geçmişte saklanacak azami uzunluğu.</summary>
    const int MaxAnswerChars = 2000;

    ConversationHistory Get(int userId, string conversationId);

    void Append(int userId, string conversationId, ConversationTurn turn);
}
