using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

/// <summary>
/// Stajyerin KENDİ profil ekranı: staj bilgileri, mentor, görev özeti ve
/// izin talepleri. Mentor notları BİLİNÇLİ olarak yok — kişi kendi hakkındaki
/// notları göremez (EmployeeNotes kararıyla aynı çizgi).
/// </summary>
public class MyInternProfileDto
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
    public string? UnitName { get; set; }         // birim opsiyonel
    public string? MentorFullName { get; set; }   // null = henüz mentor atanmamış

    /// <summary>
    /// Türetilmiş yönetici: birimin (yoksa departmanın) en kıdemli yöneticisi
    /// (UnitManagerResolver). Mentor'dan AYRI bir roldür.
    /// </summary>
    public string? ManagerFullName { get; set; }

    // Görev özeti — detay Görevlerim ekranında.
    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }

    public List<MyInternLeaveRequestDto> RecentLeaveRequests { get; set; } = [];
}

public class MyInternLeaveRequestDto
{
    public int Id { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }   // iş günü
    public LeaveStatus Status { get; set; }
    public string? Description { get; set; }   // kişinin kendi talebi — görebilir
}
