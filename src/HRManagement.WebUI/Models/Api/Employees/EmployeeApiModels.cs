namespace HRManagement.WebUI.Models.Api.Employees;

// API'nin Models/Employees tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)
//
// UserId          → giriş hesabıyla ilişkilendirme
// ManagerId       → bağlı olduğu yönetici (izin onay zinciri)
// AnnualLeaveDays → izin hakkını elle ezme; normalde boş (kıdemden hesaplanır)

public class EmployeeResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public string Position { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int? UserId { get; set; }
    public int? ManagerId { get; set; }
    public int? AnnualLeaveDays { get; set; }
    public bool IsActive { get; set; }
}

// Departmanlardan farklı olarak Create ve Update aynı alanları taşımıyor:
// güncellemede IsActive de gönderiliyor. Bu yüzden iki ayrı istek tipi var.
public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public string Position { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int? UserId { get; set; }
    public int? ManagerId { get; set; }
    public int? AnnualLeaveDays { get; set; }
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public string Position { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int? UserId { get; set; }
    public int? ManagerId { get; set; }
    public int? AnnualLeaveDays { get; set; }
    public bool IsActive { get; set; }
}
