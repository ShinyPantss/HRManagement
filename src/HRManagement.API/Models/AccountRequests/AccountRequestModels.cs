namespace HRManagement.API.Models.AccountRequests;

// Roller sayısal: 1=Admin 2=HR 3=Manager 4=Employee 5=Intern.

public sealed class CreateAccountRequestRequest
{
    public CreateAccountRequestRequest(int? employeeId, int? internId, int suggestedRole, string? note)
    {
        EmployeeId = employeeId;
        InternId = internId;
        SuggestedRole = suggestedRole;
        Note = note;
    }

    public int? EmployeeId { get; }
    public int? InternId { get; }
    public int SuggestedRole { get; }
    public string? Note { get; }
}

// Onay: hesap bilgileri Admin tarafından girilir; Role null ise talebin önerisi kullanılır.
public sealed class ApproveAccountRequestRequest
{
    public ApproveAccountRequestRequest(string username, string email, string password, int? role)
    {
        Username = username;
        Email = email;
        Password = password;
        Role = role;
    }

    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public int? Role { get; }
}

public sealed class RejectAccountRequestRequest
{
    public RejectAccountRequestRequest(string? reason)
    {
        Reason = reason;
    }

    public string? Reason { get; }
}

public sealed class AccountRequestResponse
{
    public AccountRequestResponse(
        int id, int? employeeId, int? internId, string subjectName, string subjectType,
        int requestedByUserId, string requestedByUsername, string suggestedRole,
        string? note, string status, DateTime createdAt)
    {
        Id = id;
        EmployeeId = employeeId;
        InternId = internId;
        SubjectName = subjectName;
        SubjectType = subjectType;
        RequestedByUserId = requestedByUserId;
        RequestedByUsername = requestedByUsername;
        SuggestedRole = suggestedRole;
        Note = note;
        Status = status;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public int? EmployeeId { get; }
    public int? InternId { get; }
    public string SubjectName { get; }
    public string SubjectType { get; }
    public int RequestedByUserId { get; }
    public string RequestedByUsername { get; }
    public string SuggestedRole { get; }
    public string? Note { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
