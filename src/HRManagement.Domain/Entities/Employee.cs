using HRManagement.Domain.Enums;

namespace HRManagement.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Kıdem/ünvan seviyesi (GM, GMY, Müdür...). "Pozisyon" ayrı bir alan olarak
    /// TUTULMAZ; gösterimde Departman + Kıdem'den türetilir ("IT Uzmanı" gibi).
    /// Mevcut kayıtlar için null olabilir.
    /// </summary>
    public SeniorityLevel? Seniority { get; set; }

    public int DepartmentId { get; set; }
    public int? UserId { get; set; }

    /// <summary>
    /// Bu çalışanın bağlı olduğu yönetici (yine bir Employee). Null = en tepedeki kişi.
    /// "Yöneticinin kendi ekibi" kavramı (§5.5) bu alan üzerinden kurulur.
    /// </summary>
    public int? ManagerId { get; set; }

    /// <summary>
    /// Yıllık izin hakkının ELLE GEÇERSİZ KILINMASI. Normalde null bırakılır ve
    /// hak kıdemden hesaplanır (İş Kanunu md. 53); şirket bu kişiye özel gün
    /// tanımlamışsa buraya yazılır ve hesaplama ezilir.
    /// </summary>
    public int? AnnualLeaveDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
