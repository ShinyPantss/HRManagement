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
    public int TotalDays { get; set; }   // iş günü (hafta sonu hariç)
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MedicalReport { get; set; }   // hastalık izninde dolu, diğerlerinde null
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Oluştururken tür sayısal gönderilir: 1=Yıllık, 2=Ücretsiz, 3=Hastalık.
// EmployeeId YOK: talep her zaman giriş yapan hesap için açılır; API kimliği
// JWT'den çözer (gövdeden kimlik almak istemcinin yalan söyleyebilmesi demektir).
public class CreateLeaveRequestRequest
{
    public int Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public string? MedicalReport { get; set; }   // hastalık izninde zorunlu
}

public class RejectLeaveRequestRequest
{
    public string? Reason { get; set; }
}

// "Onay Bekleyenler" satırı — API'nin PendingApprovalResponse'uyla aynı şekil.
public class PendingApprovalResponse
{
    public int Id { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // Çalışan | Stajyer
    public string Type { get; set; } = string.Empty;        // izin türü
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }
    public string Stage { get; set; } = string.Empty;       // "Yönetici onayı" | "İK onayı"
}
