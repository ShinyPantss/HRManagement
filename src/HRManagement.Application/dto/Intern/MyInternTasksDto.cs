namespace HRManagement.Application.DTOs;

/// <summary>
/// Stajyerin "Görevlerim" ekranı: kendi görevleri + mentorunun adı.
/// Mentor notları BİLİNÇLİ olarak yok — değerlendirme mahiyetindedir,
/// kişi kendi hakkındaki notları göremez (EmployeeNotes kararıyla aynı çizgi).
/// </summary>
public class MyInternTasksDto
{
    public string? MentorFullName { get; set; }   // null = henüz mentor atanmamış
    public List<InternTaskDto> Tasks { get; set; } = [];
}
