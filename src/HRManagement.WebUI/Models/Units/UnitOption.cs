namespace HRManagement.WebUI.Models.Units;

/// <summary>
/// Form birim dropdown'ı için aday. DepartmentId taşınır ki JS, seçilen departmana
/// göre süzebilsin (yönetici dropdown'ındaki ManagerCandidate ile aynı mantık).
/// </summary>
public record UnitOption(int Id, string Name, int DepartmentId);
