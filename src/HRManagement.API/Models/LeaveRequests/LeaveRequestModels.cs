namespace HRManagement.API.Models.LeaveRequests;

// Type: izin türü (1=Yıllık, 2=Ücretsiz, 3=Hastalık) — istemci int gönderir.
// EmployeeId YOK: talep her zaman giriş yapan hesabın kendisi için açılır;
// kimlik istek gövdesinden değil, imzalı JWT claim'inden okunur.
public sealed class CreateLeaveRequestRequest
{
    public CreateLeaveRequestRequest(
        int type,
        DateTime startDate,
        DateTime endDate,
        string? description)
    {
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
    }

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
        int? employeeId,
        int? internId,
        string type,
        DateTime startDate,
        DateTime endDate,
        int totalDays,
        string status,
        string? description,
        string? rejectionReason,
        DateTime createdAt)
    {
        Id = id;
        EmployeeId = employeeId;
        InternId = internId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Status = status;
        Description = description;
        RejectionReason = rejectionReason;
        CreatedAt = createdAt;
    }

    public int Id { get; }

    // Talebi açan ya çalışan ya stajyerdir; tam olarak biri dolu gelir.
    public int? EmployeeId { get; }
    public int? InternId { get; }

    public string Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int TotalDays { get; }
    public string Status { get; }
    public string? Description { get; }
    public string? RejectionReason { get; }
    public DateTime CreatedAt { get; }
}
