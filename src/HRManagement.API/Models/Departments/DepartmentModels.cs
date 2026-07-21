namespace HRManagement.API.Models.Departments;

public sealed class CreateDepartmentRequest
{
    public CreateDepartmentRequest(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }
}

public sealed class UpdateDepartmentRequest
{
    public UpdateDepartmentRequest(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }
}

public sealed class DepartmentResponse
{
    public DepartmentResponse(int id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public int Id { get; }
    public string Name { get; }
    public string? Description { get; }
}
