using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

public class UpdateUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
}