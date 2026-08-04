using HRManagement.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HRManagement.Infrastructure.Ai;

/// <summary>
/// Sohbet geçmişini SÜREÇ BELLEĞİNDE tutar. Veritabanına yazılmaz.
///
/// Neden sunucuda, tarayıcıda değil: geçmiş her istekte modele gidiyor,
/// yani doğrudan token maliyeti. Sınırı (kaç tur, tur başına kaç karakter)
/// faturayı ödeyen taraf koymalı. İstemciye bırakılsaydı aynı kuralı iki
/// yerde — bir JS'te bir C#'ta — yazmak ve senkron tutmak gerekirdi,
/// üstelik istemciden gelen sınıra zaten güvenilemezdi.
///
/// Bedeli: API yeniden başlayınca geçmiş uçar, birden fazla API örneğinde
/// paylaşılmaz. İkisi de kabul edildi — bu bir kolaylık, veri değil.
/// </summary>
public class MemoryConversationStore : IConversationStore
{
    /// <summary>
    /// KAYAN süre: her dokunuşta yeniden başlar. Terk edilmiş sohbet
    /// kendini toplar, aktif sohbet yarıda kesilmez.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;

    public MemoryConversationStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ConversationHistory Get(int userId, string conversationId)
    {
        if (!_cache.TryGetValue(Key(userId, conversationId), out ConversationState? state) || state is null)
            return new ConversationHistory([], 0);

        lock (state.Gate)
            return new ConversationHistory([.. state.Turns], state.Dropped);
    }

    public void Append(int userId, string conversationId, ConversationTurn turn)
    {
        // Boş cevap geçmişe girmemeli: API içeriksiz mesajı reddeder,
        // sonraki her soru bu yüzden patlardı.
        if (string.IsNullOrWhiteSpace(turn.Question) || string.IsNullOrWhiteSpace(turn.Answer))
            return;

        var key = Key(userId, conversationId);

        if (!_cache.TryGetValue(key, out ConversationState? state) || state is null)
            state = new ConversationState();

        lock (state.Gate)
        {
            state.Turns.Add(turn with { Answer = Clip(turn.Answer) });

            // Pencereden taşanı at ve KAÇ TANE attığımızı say — bu sayı
            // modele "bağlamın bir kısmı elinde yok" demek için kullanılıyor.
            while (state.Turns.Count > IConversationStore.MaxTurns)
            {
                state.Turns.RemoveAt(0);
                state.Dropped++;
            }
        }

        // Set her seferinde çağrılıyor: kayan süreyi tazeleyen şey bu.
        _cache.Set(key, state, new MemoryCacheEntryOptions { SlidingExpiration = Lifetime });
    }

    /// <summary>
    /// Anahtar KULLANICI KİMLİĞİNİ içerir. Yalnızca conversationId ile
    /// kurulsaydı, başkasının kimliğini tahmin eden onun sohbetini okurdu.
    /// Kimlik JWT'den gelir, istemciden değil.
    /// </summary>
    private static string Key(int userId, string conversationId) =>
        $"asst:{userId}:{conversationId}";

    private static string Clip(string answer) =>
        answer.Length <= IConversationStore.MaxAnswerChars
            ? answer
            : answer[..IConversationStore.MaxAnswerChars] + "… (geçmişte kısaltıldı)";

    private sealed class ConversationState
    {
        // IMemoryCache'in kendisi thread-safe ama içindeki List değil.
        public object Gate { get; } = new();
        public List<ConversationTurn> Turns { get; } = [];
        public int Dropped { get; set; }
    }
}
