namespace HRManagement.API.Models.LeaveRequests;

// Type: izin türü (1=Yıllık, 2=Ücretsiz, 3=Hastalık) — istemci int gönderir.
public sealed class CreateLeaveRequestRequest
{
    public CreateLeaveRequestRequest(
        int employeeId,
        int type,
        DateTime startDate,
        DateTime endDate,
        string? description)
    {
        EmployeeId = employeeId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
    }

    public int EmployeeId { get; }
    public int Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string? Description { get; }
}

// Red gerekçesi opsiyonel.
public sealed class RejectLeaveRequestRequest
{
    public RejectLeaveRequestRequest(string? reason)
    {
        Reason = reason;
    }

    public string? Reason { get; }
}

// Type ve Status okunabilir metin olarak döner (ör. "Annual", "Pending").
public sealed class LeaveRequestResponse
{
    public LeaveRequestResponse(
        int id,
        int employeeId,
        string type,
        DateTime startDate,
        DateTime endDate,
        int totalDays,
        string status,
        string? description,
        string? rejectionReason)
    {
        Id = id;
        EmployeeId = employeeId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Status = status;
        Description = description;
        RejectionReason = rejectionReason;
    }

    public int Id { get; }
    public int EmployeeId { get; }
    public string Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int TotalDays { get; }
    public string Status { get; }
    public string? Description { get; }
    public string? RejectionReason { get; }
}
