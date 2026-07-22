namespace HRManagement.WebUI.Models.Api.AccountRequests;

// API'nin Models/AccountRequests tipleriyle aynı JSON şekli. Roller sayısal:
// 1=Admin 2=HR 3=Manager 4=Employee 5=Intern.

public class AccountRequestResponse
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public string RequestedByUsername { get; set; } = string.Empty;
    public string SuggestedRole { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateAccountRequestRequest
{
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }
    public int SuggestedRole { get; set; }
    public string? Note { get; set; }
}

public class ApproveAccountRequestRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? Role { get; set; }
}

public class RejectAccountRequestRequest
{
    public string? Reason { get; set; }
}
