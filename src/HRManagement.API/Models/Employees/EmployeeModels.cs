namespace HRManagement.API.Models.Employees;

// UserId          → giriş hesabıyla ilişkilendirme (5.2; sonradan da bağlanabilir)
// ManagerId       → bağlı olduğu yönetici; izin onay zinciri buradan kurulur
// AnnualLeaveDays → izin hakkını elle ezme; normalde null (kıdemden hesaplanır)

public sealed class CreateEmployeeRequest
{
    public CreateEmployeeRequest(
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        int departmentId,
        int? userId,
        int? managerId,
        int? seniority,
        int? annualLeaveDays)
    {
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        DepartmentId = departmentId;
        UserId = userId;
        ManagerId = managerId;
        Seniority = seniority;
        AnnualLeaveDays = annualLeaveDays;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int DepartmentId { get; }
    public int? UserId { get; }
    public int? ManagerId { get; }
    public int? Seniority { get; }
    public int? AnnualLeaveDays { get; }
}

public sealed class UpdateEmployeeRequest
{
    public UpdateEmployeeRequest(
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        int departmentId,
        int? userId,
        int? managerId,
        int? seniority,
        int? annualLeaveDays,
        bool isActive)
    {
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        DepartmentId = departmentId;
        UserId = userId;
        ManagerId = managerId;
        Seniority = seniority;
        AnnualLeaveDays = annualLeaveDays;
        IsActive = isActive;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int DepartmentId { get; }
    public int? UserId { get; }
    public int? ManagerId { get; }
    public int? Seniority { get; }
    public int? AnnualLeaveDays { get; }
    public bool IsActive { get; }
}

public sealed class EmployeeResponse
{
    public EmployeeResponse(
        int id,
        string firstName,
        string lastName,
        string? nationalId,
        string email,
        string? phone,
        DateTime birthDate,
        DateTime hireDate,
        int departmentId,
        int? userId,
        int? managerId,
        int? seniority,
        int? annualLeaveDays,
        bool isActive)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        DepartmentId = departmentId;
        UserId = userId;
        ManagerId = managerId;
        Seniority = seniority;
        AnnualLeaveDays = annualLeaveDays;
        IsActive = isActive;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int DepartmentId { get; }
    public int? UserId { get; }
    public int? ManagerId { get; }
    public int? Seniority { get; }
    public int? AnnualLeaveDays { get; }
    public bool IsActive { get; }
}
