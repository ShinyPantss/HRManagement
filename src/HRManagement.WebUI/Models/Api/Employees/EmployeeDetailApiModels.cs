namespace HRManagement.WebUI.Models.Api.Employees;

// API'nin Models/Employees/EmployeeDetailModels tipleriyle aynı JSON şekli.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)
//
// NationalId  → API yalnızca HR'a doldurur; null ise satır hiç gösterilmez.
// Description → API, Manager'a null gönderir (bakiyeyi görür, gerekçeyi görmez).
// Type/Status → enum ADI ("Annual", "PendingHr"); Türkçe karşılığı view'da.

public class EmployeeDetailResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public int? Seniority { get; set; }
    public bool IsActive { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public int? ManagerId { get; set; }
    public string? ManagerFullName { get; set; }

    public int AccruedLeaveDays { get; set; }
    public int UsedLeaveDays { get; set; }
    public int RemainingLeaveDays { get; set; }

    public List<EmployeeDetailLeaveRequestResponse> RecentLeaveRequests { get; set; } = [];
    public List<EmployeeDetailTeamMemberResponse> DirectReports { get; set; } = [];
    public List<EmployeeDetailInternResponse> MentoredInterns { get; set; } = [];

    // NULL = istekçi notları göremez (kişinin kendisi) → panel hiç çizilmez.
    // Boş liste = görebilir ama henüz not yok.
    public List<EmployeeDetailNoteResponse>? Notes { get; set; }
}

public class EmployeeDetailLeaveRequestResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EmployeeDetailTeamMemberResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Seniority { get; set; }
}

public class EmployeeDetailNoteResponse
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddEmployeeNoteRequest
{
    public string Content { get; set; } = string.Empty;
}

public class EmployeeDetailInternResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
