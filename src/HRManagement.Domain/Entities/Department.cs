namespace HRManagement.Domain.Entities;

public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }    // opsiyonel

    // Audit. UTC tutulur; kullanıcıya gösterilirken çevrilir.
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }     // hiç güncellenmediyse null
}