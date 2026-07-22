namespace HRManagement.Domain.Entities;

/// <summary>
/// §5.4 — mentor notları (ör. haftalık kısa geri bildirimler).
/// EmployeeNote ile yapı olarak aynıdır ama ayrı tablodur: tek tabloda
/// birleştirmek foreign key kurmayı imkânsız kılardı.
/// </summary>
public class InternNote
{
    public int Id { get; set; }

    public int InternId { get; set; }
    public int AuthorUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
