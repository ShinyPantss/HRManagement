namespace HRManagement.Application.DTOs;

/// <summary>
/// Bekleyen talepler ekranı için — kişi ve talep eden adları JOIN ile gelir,
/// istemci "Talep #5 / Employee #9" gibi çıplak id'lerle uğraşmasın.
/// </summary>
public class AccountRequestDto
{
    public int Id { get; set; }

    // Talep kime ait: ya çalışan ya stajyer.
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }
    public string SubjectName { get; set; } = string.Empty;   // "Ad Soyad"
    public string SubjectType { get; set; } = string.Empty;   // "Çalışan" | "Stajyer"

    public int RequestedByUserId { get; set; }
    public string RequestedByUsername { get; set; } = string.Empty;

    // Pozisyon gösterimi için (Departman · Birim · Kıdem). Birim ve Kıdem opsiyonel:
    // birimi olmayan departman / stajyerde boş gelebilir.
    public string DepartmentName { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public int? Seniority { get; set; }   // çalışanın kıdemi (SeniorityLevel sayısal); stajyerde null

    public string SuggestedRole { get; set; } = string.Empty; // enum adı (arka planda tutulur)
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
