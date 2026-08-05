using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Assistant.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Assistant.Queries.AskAssistant;

/// <summary>
/// Asistanın beyni Application'dadır: hangi aracın var olduğuna, çalıştırılıp
/// çalıştırılmayacağına ve kimin sorabileceğine BURASI karar verir.
/// Infrastructure yalnızca "modelle konuşmayı" bilir.
/// </summary>
public sealed class AskAssistantQueryHandler
    : IRequestHandler<AskAssistantQuery, AssistantAnswerDto>
{
    private const string SqlToolName = "run_sql";

    /// <summary>
    /// Modele sunulan TEK araç. Bilinçli olarak tek: amaç "her veritabanı
    /// sorusuna cevap" olduğu için önceden tanımlı sorgu listesi yerine serbest
    /// SELECT tercih edildi. Bedeli, doğruluğun modele bağlı olması;
    /// karşılığı, kapsamın şemayla sınırlı olması.
    /// </summary>
    private static readonly AiTool SqlTool = new(
        Name: SqlToolName,
        Description:
            "İK veritabanında SALT OKUMA T-SQL SELECT sorgusu çalıştırır ve " +
            "satırları JSON olarak döndürür. Kullanıcının sorusu veritabanındaki " +
            "verilerle cevaplanabiliyorsa bu aracı kullan.",
        InputSchemaJson: """
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "Çalıştırılacak T-SQL SELECT sorgusu. TOP 200 içermelidir."
                }
              },
              "required": ["query"]
            }
            """);

    private readonly IUserRepository _userRepository;
    private readonly IAiAssistant _assistant;
    private readonly ISqlQueryRunner _sqlQueryRunner;
    private readonly IConversationStore _conversationStore;

    public AskAssistantQueryHandler(
        IUserRepository userRepository,
        IAiAssistant assistant,
        ISqlQueryRunner sqlQueryRunner,
        IConversationStore conversationStore)
    {
        _userRepository = userRepository;
        _assistant = assistant;
        _sqlQueryRunner = sqlQueryRunner;
        _conversationStore = conversationStore;
    }

    public async Task<AssistantAnswerDto> Handle(
        AskAssistantQuery request, CancellationToken cancellationToken)
    {
        // Yetki: serbest SELECT çalıştığı için satır bazlı görünürlük kuralları
        // (EmployeeVisibility) DEVRE DIŞI kalır — model tüm tabloyu okuyabilir.
        // Bu yüzden özellik yalnızca zaten her şeyi görebilen rollere açıktır.
        // Başka bir role açılacaksa önce görünürlük sorunu çözülmelidir.
        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);

        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        if (actor.Role is not (Role.HR or Role.Admin))
            throw new ValidationException("Asistanı kullanma yetkiniz yok.");

        var executedQueries = new List<string>();

        // Geçmiş, aktörün kimliğiyle birlikte anahtarlanır — sohbet kimliği
        // tek başına başkasının geçmişini açmaz.
        var history = _conversationStore.Get(request.ActorUserId, request.ConversationId);

        // Pencereden tur düştüyse modele haber ver. Not, sistem prompt'una
        // DEĞİL soruya ekleniyor: sistem prompt'u önbelleğe alınmış durumda ve
        // değişirse önbellek düşer (bkz. HrDatabaseSchema.TruncationNotice).
        var prompt = history.DroppedTurnCount > 0
            ? HrDatabaseSchema.TruncationNotice(history.DroppedTurnCount) + request.Question
            : request.Question;

        var answer = await _assistant.AskAsync(
            systemPrompt: HrDatabaseSchema.SystemPrompt,
            question: prompt,
            history: history.Turns,
            tools: [SqlTool],
            executeTool: (toolName, arguments, ct) =>
                ExecuteToolAsync(toolName, arguments, executedQueries, ct),
            cancellationToken: cancellationToken);

        // Geçmişe KULLANICININ sorusu yazılır, kesme notu eklenmiş hâli değil —
        // aksi hâlde not sohbette kalıcılaşır ve her turda tekrar tekrar birikir.
        _conversationStore.Append(
            request.ActorUserId,
            request.ConversationId,
            new ConversationTurn(request.Question, answer));

        return new AssistantAnswerDto
        {
            Answer = answer,
            ExecutedQueries = executedQueries
        };
    }

    /// <summary>
    /// Modelin araç çağrısını karşılar. Hata durumlarında EXCEPTION FIRLATMAZ:
    /// mesajı metin olarak döndürür ki model durumu görüp kendini düzeltebilsin
    /// (ör. yasak kelime kullandıysa sorguyu yeniden yazar). Exception fırlatmak
    /// sohbeti tamamen keserdi.
    /// </summary>
    private async Task<string> ExecuteToolAsync(
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments,
        List<string> executedQueries,
        CancellationToken cancellationToken)
    {
        if (toolName != SqlToolName)
            return $"HATA: '{toolName}' diye bir araç yok.";

        if (!arguments.TryGetValue("query", out var queryElement)
            || queryElement.ValueKind != JsonValueKind.String)
        {
            return "HATA: 'query' parametresi metin olarak verilmeli.";
        }

        var sql = queryElement.GetString() ?? string.Empty;

        // Birinci savunma katmanı. İkincisi salt okuma veritabanı kullanıcısıdır.
        // Çalıştırılan metin, DENETLENEN metnin ta kendisidir (safeSql) — ham metin
        // değil. İkisi ayrışırsa denetim atlatılabilir (bkz. SqlReadOnlyGuard).
        if (!SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out var reason))
            return $"HATA: Sorgu reddedildi — {reason}";

        // Reddedilen sorgu listeye girmez: burası "gerçekten çalışanlar" kaydıdır.
        // Kayda da çalıştırılan metin girer ki liste gerçeği yansıtsın.
        executedQueries.Add(safeSql);

        try
        {
            var result = await _sqlQueryRunner.RunReadOnlyAsync(safeSql, cancellationToken);

            if (result.RowCount == 0)
                return "Sorgu çalıştı, hiç satır dönmedi.";

            var truncationNote = result.Truncated
                ? $" (İLK {ISqlQueryRunner.MaxRows} SATIR — sonuç kırpıldı, toplam daha fazla olabilir.)"
                : string.Empty;

            return $"{result.RowCount} satır{truncationNote}:\n{result.RowsJson}";
        }
        catch (Exception ex)
        {
            // SQL sözdizimi hatası, olmayan kolon, timeout... Model mesajı okuyup
            // sorguyu düzeltebilir. İç detay sızıntısı riski kabul edilebilir:
            // bu uç yalnızca İK/Admin'e açık.
            return $"HATA: Sorgu çalıştırılamadı — {ex.Message}";
        }
    }
}
