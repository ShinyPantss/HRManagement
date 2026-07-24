namespace HRManagement.Application.DTOs;

/// <summary>
/// Birim okuma modeli. Formlardaki departmana-göre-süzülen dropdown için
/// DepartmentId taşınır (istemci JS'i seçilen departmana göre süzer).
/// </summary>
public class UnitDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
