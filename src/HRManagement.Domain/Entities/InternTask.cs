using HRManagement.Domain.Enums;

namespace HRManagement.Domain.Entities;

/// <summary>
/// §5.4 — staj süresince verilen görevler. Doküman "basit text alanı yeterli"
/// dese de ayrı tablo tercih edildi: tek metin alanında görevler sıralanamaz,
/// tamamlandı işaretlenemez ve sayılamaz.
/// </summary>
public class InternTask
{
    public int Id { get; set; }

    public int InternId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public InternTaskStatus Status { get; set; } = InternTaskStatus.Pending;

    public DateTime? DueDate { get; set; }

    /// <summary>Görevi atayan hesap (genellikle mentor).</summary>
    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
