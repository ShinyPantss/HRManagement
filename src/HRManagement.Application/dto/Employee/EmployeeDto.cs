namespace HRManagement.Application.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public int? Gender { get; set; }   // Gender enum'ının sayısal karşılığı (1=Erkek, 2=Kadın)
    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }   // departmanın alt kırılımı (Birim); opsiyonel
    public int? UserId { get; set; }
    public int? ManagerId { get; set; }
    public int? Seniority { get; set; }   // SeniorityLevel enum'ının sayısal karşılığı
    public int? AnnualLeaveDays { get; set; }
    public bool IsActive { get; set; }

    // ── Yıllık izin bakiyesi ────────────────────────────────────────────────
    // Listede gösterilir ve "birikmiş izni olanlar" filtresini besler. Hiçbiri
    // veritabanında SAKLANMAZ; hak ediş HireDate'ten, kullanım taleplerden
    // hesaplanır (bkz. LeaveEntitlement). Stajyerler bu listede yer almaz.

    /// <summary>Kıdemden hak edilen toplam gün (AnnualLeaveDays doluysa o ezer).</summary>
    public int AccruedLeaveDays { get; set; }

    /// <summary>Kullanılan + bekleyen (rezerve edilmiş) günler.</summary>
    public int UsedLeaveDays { get; set; }

    /// <summary>
    /// Kalan bakiye. NEGATİF olabilir: avans izin kullanılmışsa borç bir sonraki
    /// yıla devreder, o yüzden değeri kırpmıyoruz.
    /// </summary>
    public int RemainingLeaveDays { get; set; }
}
