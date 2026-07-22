namespace HRManagement.Application.DTOs;

public class InternDto
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
    public int? MentorId { get; set; }
    public int DepartmentId { get; set; }
    public int? UserId { get; set; }   // giriş hesabı bağı; null = hesabı yok
}