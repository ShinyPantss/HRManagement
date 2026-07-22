namespace HRManagement.API.Models.Users;

/// <summary>
/// Var olan bir kişiye hesap açma isteği. Role sayısal gelir
/// (1=Admin 2=HR 3=Manager 4=Employee 5=Intern). EmployeeId/InternId'den
/// tam olarak biri dolu olmalıdır.
/// </summary>
public sealed class CreateUserForPersonRequest
{
    public CreateUserForPersonRequest(
        string username,
        string email,
        string password,
        int role,
        int? employeeId,
        int? internId)
    {
        Username = username;
        Email = email;
        Password = password;
        Role = role;
        EmployeeId = employeeId;
        InternId = internId;
    }

    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public int Role { get; }
    public int? EmployeeId { get; }
    public int? InternId { get; }
}
