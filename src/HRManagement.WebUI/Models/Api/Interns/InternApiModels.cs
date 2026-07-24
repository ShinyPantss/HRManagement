namespace HRManagement.WebUI.Models.Api.Interns;

// API'nin Models/Interns tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)

public class InternResponse
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

    // Mentor atanmamış olabilir.
    public int? MentorId { get; set; }
    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }   // departmanın alt kırılımı (Birim); opsiyonel
    public int? UserId { get; set; }   // giriş hesabı bağı; null = hesabı yok
}

// Create ve Update aynı alanları taşıdığı için tek istek tipi yeterli.
// RequestLoginAccount yalnızca oluşturmada anlamlı; güncellemede API tarafı yok sayar.
public class InternRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public int Grade { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? MentorId { get; set; }
    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }   // departmanın alt kırılımı (Birim); opsiyonel

    // true ise stajyer eklenince Admin'e otomatik hesap talebi düşer.
    public bool RequestLoginAccount { get; set; }
}
