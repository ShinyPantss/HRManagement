namespace HRManagement.WebUI.Models.Api.Auth;

/// <summary>
/// API'nin /api/auth/login ucuna gönderilen gövde. API tarafındaki LoginRequest'in
/// kopyası — paylaşılan Contracts projesi olmadığı için JSON şekli elle senkron tutulur.
/// </summary>
public class LoginApiRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginApiResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
