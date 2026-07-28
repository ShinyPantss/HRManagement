namespace HRManagement.API.Models.Interns;

// Stajyerin "Profilim" yanıtı. Bu yanıtı yalnızca kişinin kendisi alır
// (kimlik token'dan çözülür); Type/Status enum ADI olarak taşınır.

public sealed class MyInternProfileResponse
{
    public MyInternProfileResponse(
        string firstName,
        string lastName,
        string email,
        string university,
        string major,
        int grade,
        DateTime startDate,
        DateTime endDate,
        string departmentName,
        string? unitName,
        string? mentorFullName,
        string? managerFullName,
        int totalTasks,
        int doneTasks,
        List<MyInternLeaveRequestResponse> recentLeaveRequests)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        University = university;
        Major = major;
        Grade = grade;
        StartDate = startDate;
        EndDate = endDate;
        DepartmentName = departmentName;
        UnitName = unitName;
        MentorFullName = mentorFullName;
        ManagerFullName = managerFullName;
        TotalTasks = totalTasks;
        DoneTasks = doneTasks;
        RecentLeaveRequests = recentLeaveRequests;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string University { get; }
    public string Major { get; }
    public int Grade { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string DepartmentName { get; }
    public string? UnitName { get; }
    public string? MentorFullName { get; }

    /// <summary>Türetilmiş yönetici: birimin (yoksa departmanın) en kıdemli yöneticisi.</summary>
    public string? ManagerFullName { get; }

    public int TotalTasks { get; }
    public int DoneTasks { get; }
    public List<MyInternLeaveRequestResponse> RecentLeaveRequests { get; }
}

public sealed class MyInternLeaveRequestResponse
{
    public MyInternLeaveRequestResponse(
        int id, string type, DateTime startDate, DateTime endDate,
        int totalDays, string status, string? description)
    {
        Id = id;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Status = status;
        Description = description;
    }

    public int Id { get; }
    public string Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int TotalDays { get; }
    public string Status { get; }
    public string? Description { get; }
}
