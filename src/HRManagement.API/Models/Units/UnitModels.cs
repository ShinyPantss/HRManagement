namespace HRManagement.API.Models.Units;

// Birim okuma modeli. DepartmentId taşınır: WebUI formu birim dropdown'ını
// seçilen departmana göre süzer (istemci JS).
public sealed class UnitResponse
{
    public UnitResponse(int id, int departmentId, string name)
    {
        Id = id;
        DepartmentId = departmentId;
        Name = name;
    }

    public int Id { get; }
    public int DepartmentId { get; }
    public string Name { get; }
}
