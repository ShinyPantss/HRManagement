namespace HRManagement.Domain.Entities;

/// <summary>
/// §5.2 — çalışan detay ekranındaki, HR veya yönetici tarafından girilen notlar.
/// </summary>
public class EmployeeNote
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>
    /// Notu yazan HESAP. Employee'ye değil User'a bağlanır: not girecek HR
    /// uzmanının bir çalışan kaydı olmayabilir, ama mutlaka hesabı vardır.
    /// </summary>
    public int AuthorUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
