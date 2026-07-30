namespace HRManagement.WebUI.Models.Api.Users;

// API'nin Models/Users tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)

public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
}

// Username ve parola bu uçtan geçmez: kullanıcı adı kimliğin sabit tutamağıdır,
// parola ise ayrı bir akışın konusudur.
public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
}
