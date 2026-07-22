namespace HRManagement.WebUI.Models.Api.LeaveRequests;

// API'nin Models/LeaveRequests tipleriyle aynı JSON şekline sahip olmalı.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)

public class LeaveRequestResponse
{
    public int Id { get; set; }

    // Talebi açan ya çalışan ya stajyerdir; tam olarak biri dolu gelir.
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }

    // API türü ve durumu okunabilir metin olarak döner ("Annual", "Pending" gibi).
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Oluştururken tür sayısal gönderilir: 1=Yıllık, 2=Ücretsiz, 3=Hastalık.
public class CreateLeaveRequestRequest
{
    public int EmployeeId { get; set; }
    public int Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
}

public class RejectLeaveRequestRequest
{
    public string? Reason { get; set; }
}
