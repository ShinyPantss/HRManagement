namespace HRManagement.Application.DTOs;

/// <summary>
/// Mentorun gördüğü stajyer detayı: temel bilgiler + görevler + mentor notları.
/// Bu DTO'yu YALNIZCA mentor alır (MentorshipGuard) — alan kırpma gerekmez,
/// buraya gelen her şey gösterilebilir.
/// </summary>
public class MentoredInternDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public int Grade { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>Mentor adı — HR/Admin salt-okur bakarken kimin sorumlu olduğunu görür.</summary>
    public string? MentorFullName { get; set; }

    /// <summary>Mentorun çalışan Id'si — ekranda profiline link kurmak için.</summary>
    public int? MentorEmployeeId { get; set; }

    public string? UnitName { get; set; }   // birim opsiyonel

    /// <summary>Türetilmiş yönetici: birimin (yoksa departmanın) en kıdemli yöneticisi.</summary>
    public string? ManagerFullName { get; set; }

    /// <summary>Türetilmiş yöneticinin çalışan Id'si — profiline link kurmak için.</summary>
    public int? ManagerEmployeeId { get; set; }

    public List<InternTaskDto> Tasks { get; set; } = [];
    public List<InternNoteDto> Notes { get; set; } = [];
}

public class InternTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }   // InternTaskStatus'un sayısal karşılığı
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InternNoteDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
