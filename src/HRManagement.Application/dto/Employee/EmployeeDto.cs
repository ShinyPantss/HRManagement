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
    public int DepartmentId { get; set; }
    public int? UserId { get; set; }
    public int? ManagerId { get; set; }
    public int? Seniority { get; set; }   // SeniorityLevel enum'ının sayısal karşılığı
    public int? AnnualLeaveDays { get; set; }
    public bool IsActive { get; set; }
}
