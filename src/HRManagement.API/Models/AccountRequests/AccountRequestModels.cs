namespace HRManagement.API.Models.AccountRequests;

// Roller sayısal: 1=Admin 2=HR 3=Manager 4=Employee 5=Intern.

// Onay: hesap bilgileri (kullanıcı adı/e-posta/şifre) Admin tarafından girilir.
// Rol GÖNDERİLMEZ: talebin (kişiden türetilmiş) rolü kullanılır; onayda seçilmez.
public sealed class ApproveAccountRequestRequest
{
    public ApproveAccountRequestRequest(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }

    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
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
        string? note, string status, DateTime createdAt,
        string departmentName, string? unitName, int? seniority)
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
        DepartmentName = departmentName;
        UnitName = unitName;
        Seniority = seniority;
    }

    public int Id { get; }
    public int? EmployeeId { get; }
    public int? InternId { get; }
    public string SubjectName { get; }
    public string SubjectType { get; }
    public int RequestedByUserId { get; }
    public string RequestedByUsername { get; }
    public string SuggestedRole { get; }

    // Pozisyon gösterimi (Departman · Birim · Kıdem). Birim/Kıdem opsiyonel.
    public string DepartmentName { get; }
    public string? UnitName { get; }
    public int? Seniority { get; }
    public string? Note { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
