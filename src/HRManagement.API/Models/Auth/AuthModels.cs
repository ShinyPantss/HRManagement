namespace HRManagement.API.Models.Auth;

public sealed class LoginRequest
{
    public LoginRequest(string usernameOrEmail, string password)
    {
        UsernameOrEmail = usernameOrEmail;
        Password = password;
    }

    public string UsernameOrEmail { get; }
    public string Password { get; }
}

public sealed class LoginResponse
{
    public LoginResponse(string token, string username, string role)
    {
        Token = token;
        Username = username;
        Role = role;
    }

    public string Token { get; }
    public string Username { get; }
    public string Role { get; }
}

public sealed class CurrentUserResponse
{
    public CurrentUserResponse(int id, string username, string role)
    {
        Id = id;
        Username = username;
        Role = role;
    }

    public int Id { get; }
    public string Username { get; }
    public string Role { get; }
}
