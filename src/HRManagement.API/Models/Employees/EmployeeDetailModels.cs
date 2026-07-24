namespace HRManagement.API.Models.Employees;

// Detay/profil yanıtı. Hassas alan kırpma (TC yalnız HR, izin açıklaması
// Manager'a kapalı) Application'da yapılır; buraya gelen her şey gösterilebilir.
// Type/Status enum ADI olarak taşınır ("Annual", "Pending") — sayı sözleşmesi
// istemciye sızmaz, LeaveRequests uçlarıyla aynı yaklaşım.

public sealed class EmployeeDetailResponse
{
    public EmployeeDetailResponse(
        int id,
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        int? seniority,
        bool isActive,
        int departmentId,
        string departmentName,
        int? managerId,
        string? managerFullName,
        int accruedLeaveDays,
        int usedLeaveDays,
        int remainingLeaveDays,
        List<EmployeeDetailLeaveRequestResponse> recentLeaveRequests,
        List<EmployeeDetailTeamMemberResponse> directReports,
        List<EmployeeDetailInternResponse> mentoredInterns,
        List<EmployeeDetailNoteResponse>? notes)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        Seniority = seniority;
        IsActive = isActive;
        DepartmentId = departmentId;
        DepartmentName = departmentName;
        ManagerId = managerId;
        ManagerFullName = managerFullName;
        AccruedLeaveDays = accruedLeaveDays;
        UsedLeaveDays = usedLeaveDays;
        RemainingLeaveDays = remainingLeaveDays;
        RecentLeaveRequests = recentLeaveRequests;
        DirectReports = directReports;
        MentoredInterns = mentoredInterns;
        Notes = notes;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int? Seniority { get; }
    public bool IsActive { get; }
    public int DepartmentId { get; }
    public string DepartmentName { get; }
    public int? ManagerId { get; }
    public string? ManagerFullName { get; }
    public int AccruedLeaveDays { get; }
    public int UsedLeaveDays { get; }
    public int RemainingLeaveDays { get; }
    public List<EmployeeDetailLeaveRequestResponse> RecentLeaveRequests { get; }
    public List<EmployeeDetailTeamMemberResponse> DirectReports { get; }
    public List<EmployeeDetailInternResponse> MentoredInterns { get; }

    /// <summary>NULL = istekçi notları göremez (kişinin kendisi); boş liste = not yok.</summary>
    public List<EmployeeDetailNoteResponse>? Notes { get; }
}

public sealed class EmployeeDetailLeaveRequestResponse
{
    public EmployeeDetailLeaveRequestResponse(
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

public sealed class EmployeeDetailTeamMemberResponse
{
    public EmployeeDetailTeamMemberResponse(int id, string fullName, int? seniority)
    {
        Id = id;
        FullName = fullName;
        Seniority = seniority;
    }

    public int Id { get; }
    public string FullName { get; }
    public int? Seniority { get; }
}

public sealed class EmployeeDetailNoteResponse
{
    public EmployeeDetailNoteResponse(int id, string authorName, string content, DateTime createdAt)
    {
        Id = id;
        AuthorName = authorName;
        Content = content;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public string AuthorName { get; }
    public string Content { get; }
    public DateTime CreatedAt { get; }
}

public sealed class AddEmployeeNoteRequest
{
    public AddEmployeeNoteRequest(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public sealed class EmployeeDetailInternResponse
{
    public EmployeeDetailInternResponse(
        int id, string fullName, string university, DateTime startDate, DateTime endDate)
    {
        Id = id;
        FullName = fullName;
        University = university;
        StartDate = startDate;
        EndDate = endDate;
    }

    public int Id { get; }
    public string FullName { get; }
    public string University { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
}
