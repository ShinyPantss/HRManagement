namespace HRManagement.API.Models.Employees;

// UserId          → giriş hesabıyla ilişkilendirme (5.2; sonradan da bağlanabilir)
// ManagerId       → bağlı olduğu yönetici; izin onay zinciri buradan kurulur
// AnnualLeaveDays → izin hakkını elle ezme; normalde null (kıdemden hesaplanır)
// UnitId          → departmanın alt kırılımı (Birim); opsiyonel, departmana ait olmalı
// Gender          → 1=Erkek, 2=Kadın (yeni kayıtta zorunlu; Application validator)
// RequestLoginAccount → true ise çalışan eklenince Admin'e otomatik hesap talebi düşer

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
        int? gender,
        int departmentId,
        int? unitId,
        int? userId,
        int? managerId,
        int? seniority,
        int? annualLeaveDays,
        bool requestLoginAccount)
    {
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        Gender = gender;
        DepartmentId = departmentId;
        UnitId = unitId;
        UserId = userId;
        ManagerId = managerId;
        Seniority = seniority;
        AnnualLeaveDays = annualLeaveDays;
        RequestLoginAccount = requestLoginAccount;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int? Gender { get; }
    public int DepartmentId { get; }
    public int? UnitId { get; }
    public int? UserId { get; }
    public int? ManagerId { get; }
    public int? Seniority { get; }
    public int? AnnualLeaveDays { get; }
    public bool RequestLoginAccount { get; }
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
        int? gender,
        int departmentId,
        int? unitId,
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
        Gender = gender;
        DepartmentId = departmentId;
        UnitId = unitId;
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
    public int? Gender { get; }
    public int DepartmentId { get; }
    public int? UnitId { get; }
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
        int? gender,
        int departmentId,
        int? unitId,
        int? userId,
        int? managerId,
        int? seniority,
        int? annualLeaveDays,
        bool isActive,
        int accruedLeaveDays,
        int usedLeaveDays,
        int remainingLeaveDays)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HireDate = hireDate;
        Gender = gender;
        DepartmentId = departmentId;
        UnitId = unitId;
        UserId = userId;
        ManagerId = managerId;
        Seniority = seniority;
        AnnualLeaveDays = annualLeaveDays;
        IsActive = isActive;
        AccruedLeaveDays = accruedLeaveDays;
        UsedLeaveDays = usedLeaveDays;
        RemainingLeaveDays = remainingLeaveDays;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? NationalId { get; }
    public string Email { get; }
    public string? Phone { get; }
    public DateTime BirthDate { get; }
    public DateTime HireDate { get; }
    public int? Gender { get; }
    public int DepartmentId { get; }
    public int? UnitId { get; }
    public int? UserId { get; }
    public int? ManagerId { get; }
    public int? Seniority { get; }
    public int? AnnualLeaveDays { get; }
    public bool IsActive { get; }

    // Yıllık izin bakiyesi — veritabanında saklanmaz, her istekte hesaplanır.
    // RemainingLeaveDays NEGATİF olabilir (avans izin borcu devreder).
    public int AccruedLeaveDays { get; }
    public int UsedLeaveDays { get; }
    public int RemainingLeaveDays { get; }
}
