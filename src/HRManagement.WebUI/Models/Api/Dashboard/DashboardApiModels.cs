namespace HRManagement.WebUI.Models.Api.Dashboard;

// API'nin Models/Dashboard tipleriyle aynı JSON şekli (Contracts projesi yok — elle senkron).
public class HrDashboardResponse
{
    public int TotalActiveEmployees { get; set; }
    public int OnLeaveNowCount { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ActiveInterns { get; set; }

    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int GenderUnspecifiedCount { get; set; }

    public List<DepartmentHeadcountResponse> DepartmentHeadcounts { get; set; } = [];
    public List<OnLeaveNowResponse> OnLeaveNow { get; set; } = [];
}

public class DepartmentHeadcountResponse
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OnLeaveNowResponse
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // Çalışan | Stajyer
    public string TypeName { get; set; } = string.Empty;     // Annual | Unpaid | Sick
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
