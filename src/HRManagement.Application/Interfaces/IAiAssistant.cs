using System.Text.Json;

namespace HRManagement.Application.Interfaces;

/// <summary>
/// Yapay zekâ asistanının kullanabileceği bir aracın (fonksiyonun) tanımı.
///
/// <para><see cref="InputSchemaJson"/> ham JSON Schema metnidir — Application'ın
/// hiçbir şema/serileştirme kütüphanesine bağlanmaması için string taşınır.
/// Şemayı sağlayıcının beklediği şekle çevirmek Infrastructure'ın işidir.</para>
/// </summary>
/// <param name="Name">Modelin çağırırken kullanacağı ad (ör. "search_employees").</param>
/// <param name="Description">
/// Modelin bu aracı NE ZAMAN çağıracağına karar vermesini sağlayan açıklama.
/// Ne yaptığını değil, hangi soruda kullanılacağını yaz — seçim kalitesini asıl bu belirler.
/// </param>
/// <param name="InputSchemaJson">Parametrelerin JSON Schema tanımı.</param>
public sealed record AiTool(string Name, string Description, string InputSchemaJson);

/// <summary>
/// Modelin seçtiği aracı ÇALIŞTIRAN geri çağırım.
///
/// Bu delegate mimarinin kilit noktası: aracı çalıştırma kararı ve yürütmesi
/// Application'da kalır, Infrastructure yalnızca "model şunu istedi" deyip çağırır.
/// Böylece hangi use-case'in koşacağını, yetkinin nasıl denetleneceğini yapay zekâ
/// sağlayıcısının yanındaki kod DEĞİL, iş katmanı belirler.
///
/// Dönen string modele geri verilir; modelin okuyabileceği herhangi bir metin
/// (genelde JSON) olabilir.
/// </summary>
public delegate Task<string> AiToolExecutor(
    string toolName,
    IReadOnlyDictionary<string, JsonElement> arguments,
    CancellationToken cancellationToken);

/// <summary>
/// Doğal dil asistanı. Arkasında hangi model/sağlayıcı olduğu bir INFRASTRUCTURE
/// detayıdır; Application yalnızca bu sözleşmeyi görür.
///
/// ÖNEMLİ — güvenlik sözleşmesi: bu arayüz "kim soruyor" bilgisini TAŞIMAZ.
/// İstekçinin kimliği araç yürütücüsünün (<see cref="AiToolExecutor"/>) closure'ında,
/// imzalı JWT claim'inden gelen değerle sabitlenir. Model kendi kimliğini
/// bildiremez, dolayısıyla "ben Admin'im" diyerek yetki yükseltemez.
/// </summary>
public interface IAiAssistant
{
    /// <param name="systemPrompt">Asistanın rolü ve sınırları.</param>
    /// <param name="question">Kullanıcının doğal dildeki sorusu.</param>
    /// <param name="history">
    /// Önceki soru-cevap turları, eskiden yeniye. Yalnızca METİN çiftleri taşınır;
    /// araç çağrıları ve sonuçları saklanmaz. Sebep: eski sorgu sonuçları hem
    /// bağlamı şişirir hem BAYATLAR — model dünkü veriyle bugünkü soruyu
    /// cevaplamamalı. Bağlamı görüp veriyi yeniden sorgulaması doğrusu.
    /// </param>
    /// <param name="tools">Modele sunulacak araçlar. Hepsi SALT OKUMA olmalıdır.</param>
    /// <param name="executeTool">Aracı çalıştıran geri çağırım (Application sağlar).</param>
    Task<string> AskAsync(
        string systemPrompt,
        string question,
        IReadOnlyList<ConversationTurn> history,
        IReadOnlyList<AiTool> tools,
        AiToolExecutor executeTool,
        CancellationToken cancellationToken);
}
