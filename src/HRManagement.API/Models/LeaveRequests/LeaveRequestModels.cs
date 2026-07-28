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
        string? description,
        string? medicalReport)
    {
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
        MedicalReport = medicalReport;
    }

    public int Type { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string? Description { get; }
    public string? MedicalReport { get; }   // hastalık izninde zorunlu
}

// "Onay Bekleyenler" satırı — yalnızca görüntü alanları (yetki süzmesi Application'da).
public sealed class PendingApprovalResponse
{
    public PendingApprovalResponse(
        int id, string subjectName, string subjectType, string type,
        DateTime startDate, DateTime endDate, int workingDays, string stage)
    {
        Id = id;
        SubjectName = subjectName;
        SubjectType = subjectType;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        WorkingDays = workingDays;
        Stage = stage;
    }

    public int Id { get; }
    public string SubjectName { get; }
    public string SubjectType { get; }   // Çalışan | Stajyer
    public string Type { get; }          // izin türü
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int WorkingDays { get; }
    public string Stage { get; }         // "Yönetici onayı" | "İK onayı"
}

// "İzin Geçmişi" satırı — HR/Admin TÜM izinleri her durumda gözlemler (salt görüntü).
public sealed class LeaveHistoryResponse
{
    public LeaveHistoryResponse(
        int id, string subjectName, string subjectType, string type,
        DateTime startDate, DateTime endDate, int workingDays, string status,
        string? description, string? rejectionReason, DateTime createdAt)
    {
        Id = id;
        SubjectName = subjectName;
        SubjectType = subjectType;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        WorkingDays = workingDays;
        Status = status;
        Description = description;
        RejectionReason = rejectionReason;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public string SubjectName { get; }
    public string SubjectType { get; }   // Çalışan | Stajyer
    public string Type { get; }          // izin türü (Annual/Unpaid/Sick)
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int WorkingDays { get; }
    public string Status { get; }        // Pending | PendingHr | Approved | Rejected
    public string? Description { get; }
    public string? RejectionReason { get; }
    public DateTime CreatedAt { get; }
}

// Tek izin talebinin DETAY yanıtı — kişi, tarih/gün, durum + iki aşamalı onayın izi
// (kim, ne zaman). CanActNow: giriş yapan bu talebi şu an onaylayıp reddedebilir mi.
public sealed class LeaveDetailResponse
{
    public LeaveDetailResponse(
        int id, string subjectName, string subjectType, string type,
        DateTime startDate, DateTime endDate, int workingDays, string status,
        string? description, string? medicalReport, string? rejectionReason, DateTime createdAt,
        string? managerApprovedByName, DateTime? managerApprovedAt,
        string? hrApprovedByName, DateTime? hrApprovedAt,
        string? rejectedByName, DateTime? rejectedAt, bool canActNow)
    {
        Id = id;
        SubjectName = subjectName;
        SubjectType = subjectType;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        WorkingDays = workingDays;
        Status = status;
        Description = description;
        MedicalReport = medicalReport;
        RejectionReason = rejectionReason;
        CreatedAt = createdAt;
        ManagerApprovedByName = managerApprovedByName;
        ManagerApprovedAt = managerApprovedAt;
        HrApprovedByName = hrApprovedByName;
        HrApprovedAt = hrApprovedAt;
        RejectedByName = rejectedByName;
        RejectedAt = rejectedAt;
        CanActNow = canActNow;
    }

    public int Id { get; }
    public string SubjectName { get; }
    public string SubjectType { get; }   // Çalışan | Stajyer
    public string Type { get; }          // Annual | Unpaid | Sick
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int WorkingDays { get; }
    public string Status { get; }        // Pending | PendingHr | Approved | Rejected
    public string? Description { get; }
    public string? MedicalReport { get; }
    public string? RejectionReason { get; }
    public DateTime CreatedAt { get; }
    public string? ManagerApprovedByName { get; }
    public DateTime? ManagerApprovedAt { get; }
    public string? HrApprovedByName { get; }
    public DateTime? HrApprovedAt { get; }
    public string? RejectedByName { get; }
    public DateTime? RejectedAt { get; }
    public bool CanActNow { get; }
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
        string? medicalReport,
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
        MedicalReport = medicalReport;
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
    public int TotalDays { get; }   // iş günü (hafta sonu hariç)
    public string Status { get; }
    public string? Description { get; }
    public string? MedicalReport { get; }
    public string? RejectionReason { get; }
    public DateTime CreatedAt { get; }
}
