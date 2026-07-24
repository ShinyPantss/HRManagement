namespace HRManagement.Domain.Entities;

/// <summary>
/// Departmanın alt kırılımı (Birim). Bir departmanın altında birden çok birim olur
/// (ör. "Bilgi Teknolojileri" → "Sistem ve Network"). Her birim tam bir departmana bağlıdır.
/// </summary>
public class Unit
{
    public int Id { get; set; }

    // Hangi departmanın altında (bire-çok).
    public int DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    // Audit. UTC tutulur; kullanıcıya gösterilirken çevrilir.
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
