namespace HRManagement.WebUI.Models.Api.Units;

// API'nin Models/Units tipiyle aynı JSON şekli. DepartmentId, formda birim
// dropdown'ını seçilen departmana göre süzmek için taşınır.
public class UnitResponse
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
