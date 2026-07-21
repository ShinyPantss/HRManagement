namespace HRManagement.WebUI.Models.Api.Departments;

// API'nin Models/Departments tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)

public class DepartmentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Create ve Update aynı alanları taşıdığı için tek istek tipi yeterli.
public class DepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
