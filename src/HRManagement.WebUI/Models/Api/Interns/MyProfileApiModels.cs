namespace HRManagement.WebUI.Models.Api.Interns;

// API'nin Models/Interns/MyProfileModels tipleriyle aynı JSON şekli.
// Type/Status enum ADI gelir; Türkçe karşılıkları view'da.

public class MyInternProfileResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public int Grade { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string DepartmentName { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public string? MentorFullName { get; set; }

    // Türetilmiş yönetici: birimin (yoksa departmanın) en kıdemli yöneticisi.
    public string? ManagerFullName { get; set; }

    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }

    public List<MyInternLeaveRequestResponse> RecentLeaveRequests { get; set; } = [];
}

public class MyInternLeaveRequestResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}
