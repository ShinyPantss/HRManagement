using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using HRManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HRManagement.Infrastructure.Ai;

/// <summary>
/// <see cref="IAiAssistant"/>'ın Claude ile gerçeklenmesi.
///
/// Bu sınıf HANGİ aracın çalışacağını bilmez — yalnızca modelin "şunu çağır"
/// dediğini alıp Application'ın verdiği geri çağırıma iletir. Sağlayıcı
/// değişirse (başka model, yerel bir model) yalnızca bu dosya değişir;
/// Application'da tek satır dokunulmaz. JwtTokenGenerator ile aynı ilke.
///
/// <see cref="AskAsync"/> protokolün İSKELETİNİ tutar (gönder → araç istendi mi →
/// çalıştır → geri gönder → tekrarla); her adımın ayrıntısı adı olan bir private
/// metoda iner. Böylece akışı okumak için ayrıntıları okumak gerekmez.
/// </summary>
public class ClaudeAssistant : IAiAssistant
{
    private const string ModelId = "claude-sonnet-5";
    private const int MaxTokens = 16000;

    /// <summary>
    /// Model → araç → model turlarının üst sınırı. Model bir sorguyu düzeltmek
    /// için birkaç deneme yapabilmeli (ör. önce departman listesini çeker, sonra
    /// asıl sorguyu yazar) ama sonsuz döngüye girmemeli.
    /// </summary>
    private const int MaxToolIterations = 6;

    private const string RefusalMessage = "Bu soru güvenlik nedeniyle yanıtlanamadı.";

    private const string TooManyStepsMessage =
        "Soru çok fazla adım gerektirdi ve tamamlanamadı. Daha dar bir soru sormayı deneyin.";

    // AnthropicClient içinde HttpClient taşır; her istekte yenisini üretmek
    // soket tükenmesine yol açar. Lazy + Singleton kayıt: yapılandırma yalnızca
    // asistan ilk kez kullanıldığında okunur, uygulama açılışı etkilenmez.
    private readonly Lazy<AnthropicClient> _client;

    public ClaudeAssistant(IConfiguration configuration)
    {
        _client = new Lazy<AnthropicClient>(() =>
        {
            var apiKey = configuration["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException(
                    "'Anthropic:ApiKey' yapılandırması bulunamadı. user-secrets ile verin: " +
                    "dotnet user-secrets set \"Anthropic:ApiKey\" \"<anahtar>\" " +
                    "--project src/HRManagement.API");

            return new AnthropicClient { ApiKey = apiKey };
        });
    }

    public async Task<string> AskAsync(
        string systemPrompt,
        string question,
        IReadOnlyList<ConversationTurn> history,
        IReadOnlyList<AiTool> tools,
        AiToolExecutor executeTool,
        CancellationToken cancellationToken)
    {
        ToolUnion[] anthropicTools = [.. tools.Select(ToAnthropicTool)];
        var systemBlocks = BuildSystemBlocks(systemPrompt);
        var messages = BuildInitialMessages(history, question);

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            var response = await SendAsync(systemBlocks, anthropicTools, messages);

            // Güvenlik sınıflandırıcısı isteği reddettiyse içerik boş/kısmi olur;
            // content'i okumadan önce bakmak gerekir.
            if (response.StopReason == "refusal")
                return RefusalMessage;

            // Model araç istemiyorsa iş bitti: metin cevabı döner.
            if (response.StopReason != "tool_use")
                return ExtractText(response);

            var (assistantContent, toolUses) = SplitContent(response);
            var toolResults = await RunToolsAsync(toolUses, executeTool, cancellationToken);

            // Asistanın turu + araç sonuçları geçmişe eklenir; döngü modele geri döner.
            // Araç sonuçlarının HEPSİ tek bir user mesajında gitmeli — bölünürse
            // API eşleşmeyen tool_use bloğu yüzünden isteği reddeder.
            messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });
            messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
        }

        return TooManyStepsMessage;
    }

    /// <summary>
    /// Sistem prompt'unu önbellek işaretiyle sarar.
    ///
    /// Sistem prompt'u (şema + tuzaklar + SQL şablonu) ve araç tanımları HER
    /// istekte harfi harfine aynı ve büyük — kabaca 2500 token. Önbellek
    /// işareti bunları bir kez işletip sonraki isteklerde temel fiyatın
    /// ~0.1'ine okutur; yazma 1.25×, yani İKİ istekte başa baş.
    ///
    /// İşaret yalnızca system'e konuyor: araçlar istemde system'den ÖNCE
    /// yer aldığı için tek işaret ikisini birden kapsıyor.
    ///
    /// DİKKAT: prompt'un tek bayti değişirse önbellek komple düşer. Bu yüzden
    /// SystemPrompt bir const — içine tarih/kullanıcı adı gibi değişen bir şey
    /// ENJEKTE EDİLMEMELİ, yoksa her istek yeniden yazma ücreti öder.
    /// </summary>
    private static List<TextBlockParam> BuildSystemBlocks(string systemPrompt) =>
    [
        new() { Text = systemPrompt, CacheControl = new CacheControlEphemeral() }
    ];

    /// <summary>
    /// Geçmişi, modelin beklediği sıralı user/assistant dizisine açar ve sona
    /// güncel soruyu ekler.
    ///
    /// Araç turları saklanmadığı için model eski sorguların sonuçlarını GÖRMEZ —
    /// bağlamı görür, veriyi gerekiyorsa yeniden sorgular.
    /// </summary>
    private static List<MessageParam> BuildInitialMessages(
        IReadOnlyList<ConversationTurn> history, string question)
    {
        var messages = new List<MessageParam>();

        foreach (var turn in history)
        {
            messages.Add(new() { Role = Role.User, Content = turn.Question });
            messages.Add(new() { Role = Role.Assistant, Content = turn.Answer });
        }

        messages.Add(new() { Role = Role.User, Content = question });

        return messages;
    }

    private Task<Message> SendAsync(
        List<TextBlockParam> systemBlocks,
        ToolUnion[] tools,
        List<MessageParam> messages) =>
        _client.Value.Messages.Create(new MessageCreateParams
        {
            Model = ModelId,
            MaxTokens = MaxTokens,
            System = systemBlocks,
            Tools = tools,
            Messages = messages
        });

    /// <summary>
    /// Modelin cevabını ikiye ayırır: geçmişe AYNEN geri gönderilecek blok
    /// listesi, ve çalıştırılacak araç çağrıları.
    ///
    /// Blok SIRASI korunur — API, asistan turunu geldiği düzende geri bekler.
    /// </summary>
    private static (List<ContentBlockParam> AssistantContent, List<ToolUseBlock> ToolUses)
        SplitContent(Message response)
    {
        var assistantContent = new List<ContentBlockParam>();
        var toolUses = new List<ToolUseBlock>();

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? text))
            {
                assistantContent.Add(new TextBlockParam { Text = text.Text });
            }
            else if (block.TryPickThinking(out ThinkingBlock? thinking))
            {
                // İmza AYNEN korunmalı — API kurcalanmış düşünce bloğunu reddeder.
                assistantContent.Add(new ThinkingBlockParam
                {
                    Thinking = thinking.Thinking,
                    Signature = thinking.Signature
                });
            }
            else if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
            {
                assistantContent.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
            }
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
            {
                assistantContent.Add(new ToolUseBlockParam
                {
                    ID = toolUse.ID,
                    Name = toolUse.Name,
                    Input = toolUse.Input
                });

                toolUses.Add(toolUse);
            }
        }

        return (assistantContent, toolUses);
    }

    /// <summary>
    /// Araç çağrılarını çalıştırıp sonuç bloklarını üretir.
    ///
    /// Yürütücü Application'dan gelir; hataları EXCEPTION olarak değil METİN
    /// olarak döndürür ki model durumu görüp kendini düzeltebilsin (ör. yasak
    /// kelime kullandıysa sorguyu yeniden yazar).
    ///
    /// Çağrılar SIRAYLA çalışır. Model tek turda birden fazla araç isteyebilir
    /// ve bunlar birbirinden bağımsızdır — paralel çalıştırmak süreyi kısaltır.
    /// Yapılmadı çünkü yürütücü paylaşılan bir listeye yazıyor (çalıştırılan
    /// sorgu kaydı) ve o liste eşzamanlı yazmaya hazır değil; ikisi birlikte
    /// değiştirilmeli.
    /// </summary>
    private static async Task<List<ContentBlockParam>> RunToolsAsync(
        List<ToolUseBlock> toolUses,
        AiToolExecutor executeTool,
        CancellationToken cancellationToken)
    {
        var toolResults = new List<ContentBlockParam>(toolUses.Count);

        foreach (var toolUse in toolUses)
        {
            var result = await executeTool(toolUse.Name, toolUse.Input, cancellationToken);

            toolResults.Add(new ToolResultBlockParam
            {
                ToolUseID = toolUse.ID,
                Content = result
            });
        }

        return toolResults;
    }

    /// <summary>
    /// <see cref="AiTool"/>'daki ham JSON Schema metnini SDK'nın beklediği şekle çevirir.
    /// Application'ın şema kütüphanesine bağlanmaması için çeviri burada yapılır.
    /// </summary>
    private static ToolUnion ToAnthropicTool(AiTool tool)
    {
        using var document = JsonDocument.Parse(tool.InputSchemaJson);
        var root = document.RootElement;

        var properties = new Dictionary<string, JsonElement>();
        if (root.TryGetProperty("properties", out var propertiesElement))
        {
            foreach (var property in propertiesElement.EnumerateObject())
            {
                // Clone() şart: JsonDocument dispose edilince klonlanmamış
                // JsonElement'ler geçersiz belleğe bakar.
                properties[property.Name] = property.Value.Clone();
            }
        }

        var required = new List<string>();
        if (root.TryGetProperty("required", out var requiredElement))
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.GetString() is { } name)
                    required.Add(name);
            }
        }

        return new ToolUnion(new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new() { Properties = properties, Required = required }
        });
    }

    /// <summary>
    /// Yanıttaki metin bloklarını birleştirir. Düşünce blokları atlanır —
    /// kullanıcıya modelin iç muhakemesi değil, sonucu gösterilir.
    /// </summary>
    private static string ExtractText(Message response)
    {
        var paragraphs = response.Content
            .Select(block => block.Value)
            .OfType<TextBlock>()
            .Select(text => text.Text);

        return string.Join("\n", paragraphs).Trim();
    }
}
