namespace HRManagement.API.Models.Dashboard;

// İK/Admin ana sayfa panosu yanıtı — hepsi görüntü alanı (yetki API rol kapısında).
public sealed class HrDashboardResponse
{
    public HrDashboardResponse(
        int totalActiveEmployees,
        int onLeaveNowCount,
        int pendingLeaveRequests,
        int activeInterns,
        int maleCount,
        int femaleCount,
        int genderUnspecifiedCount,
        List<DepartmentHeadcountResponse> departmentHeadcounts,
        List<OnLeaveNowResponse> onLeaveNow)
    {
        TotalActiveEmployees = totalActiveEmployees;
        OnLeaveNowCount = onLeaveNowCount;
        PendingLeaveRequests = pendingLeaveRequests;
        ActiveInterns = activeInterns;
        MaleCount = maleCount;
        FemaleCount = femaleCount;
        GenderUnspecifiedCount = genderUnspecifiedCount;
        DepartmentHeadcounts = departmentHeadcounts;
        OnLeaveNow = onLeaveNow;
    }

    public int TotalActiveEmployees { get; }
    public int OnLeaveNowCount { get; }
    public int PendingLeaveRequests { get; }
    public int ActiveInterns { get; }
    public int MaleCount { get; }
    public int FemaleCount { get; }
    public int GenderUnspecifiedCount { get; }
    public List<DepartmentHeadcountResponse> DepartmentHeadcounts { get; }
    public List<OnLeaveNowResponse> OnLeaveNow { get; }
}

public sealed class DepartmentHeadcountResponse
{
    public DepartmentHeadcountResponse(string departmentName, int count)
    {
        DepartmentName = departmentName;
        Count = count;
    }

    public string DepartmentName { get; }
    public int Count { get; }
}

public sealed class OnLeaveNowResponse
{
    public OnLeaveNowResponse(
        string subjectName, string subjectType, string typeName, DateTime startDate, DateTime endDate)
    {
        SubjectName = subjectName;
        SubjectType = subjectType;
        TypeName = typeName;
        StartDate = startDate;
        EndDate = endDate;
    }

    public string SubjectName { get; }
    public string SubjectType { get; }   // Çalışan | Stajyer
    public string TypeName { get; }      // Annual | Unpaid | Sick
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
}
